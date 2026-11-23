using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace DreamDungeon
{
    public sealed class AnimationTestSceneController : MonoBehaviour
    {
        [SerializeField] private CharacterAnimationDatabase database;
        [SerializeField] private Transform previewSpawnRoot;
        [SerializeField] private Text characterNameText;
        [SerializeField] private Text characterMetaText;
        [SerializeField] private Text currentAnimationText;
        [SerializeField] private Dropdown characterDropdown;

        private GameObject currentInstance;
        private Animator currentAnimator;
        private SpriteRenderer currentRenderer;
        private int currentIndex;
        private Coroutine playAllCoroutine;

        private void Awake()
        {
            if (database == null)
            {
                database = Resources.Load<CharacterAnimationDatabase>("DreamDungeon/CharacterAnimationDatabase");
            }

            EnsureCameraAndSceneSetup();
            EnsureUI();
        }

        private void Start()
        {
            PopulateCharacters();
            if (database != null && database.Entries.Count > 0)
            {
                SelectCharacter(0);
            }
        }

        private void EnsureCameraAndSceneSetup()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                camObj.tag = "MainCamera";
                cam = camObj.GetComponent<Camera>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 3.2f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.07f, 0.11f, 1f);
            cam.transform.position = new Vector3(0f, 0.2f, -10f);

            if (previewSpawnRoot == null)
            {
                GameObject root = GameObject.Find("PreviewSpawnRoot");
                if (root == null)
                {
                    root = new GameObject("PreviewSpawnRoot");
                    root.transform.position = new Vector3(0f, -0.35f, 0f);
                }
                previewSpawnRoot = root.transform;
            }

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                var module = es.GetComponent<InputSystemUIInputModule>();
                if (module != null) module.AssignDefaultActions();
            }
        }

        private void EnsureUI()
        {
            if (characterNameText != null && currentAnimationText != null) return;

            Canvas existingCanvas = FindAnyObjectByType<Canvas>();
            if (existingCanvas != null)
            {
                Transform safe = existingCanvas.transform.Find("SafeAreaRoot") ?? existingCanvas.transform;
                characterNameText = safe.Find("CharacterNameText")?.GetComponent<Text>();
                characterMetaText = safe.Find("CharacterMetaText")?.GetComponent<Text>();
                currentAnimationText = safe.Find("CurrentAnimationText")?.GetComponent<Text>();
                characterDropdown = safe.Find("CharacterDropdown")?.GetComponent<Dropdown>();

                BindButtons(safe);
                if (characterNameText != null && currentAnimationText != null) return;
            }

            BuildRuntimeUI();
        }

        private void BindButtons(Transform root)
        {
            BindButton(root, "PrevCharacterButton", SelectPreviousCharacter);
            BindButton(root, "NextCharacterButton", SelectNextCharacter);
            BindButton(root, "IdleButton", PlayIdle);
            BindButton(root, "WalkButton", PlayWalk);
            BindButton(root, "AttackButton", PlayAttack);
            BindButton(root, "HitButton", PlayHit);
            BindButton(root, "DeathButton", PlayDeath);
            BindButton(root, "PlayAllButton", PlayAllSequence);

            if (characterDropdown != null)
            {
                characterDropdown.onValueChanged.RemoveAllListeners();
                characterDropdown.onValueChanged.AddListener(SelectCharacter);
            }
        }

        private static void BindButton(Transform root, string buttonName, UnityEngine.Events.UnityAction action)
        {
            Button btn = root.Find(buttonName)?.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(action);
            }
        }

        private void BuildRuntimeUI()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject canvasObj = new GameObject("AnimationTestCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GameObject safeObj = new GameObject("SafeAreaRoot", typeof(RectTransform), typeof(SafeAreaController));
            safeObj.transform.SetParent(canvasObj.transform, false);
            RectTransform safe = safeObj.GetComponent<RectTransform>();
            safe.anchorMin = Vector2.zero;
            safe.anchorMax = Vector2.one;
            safe.offsetMin = Vector2.zero;
            safe.offsetMax = Vector2.zero;

            CreateLabel(safe, font, "HeaderTitle", "DREAM DUNGEON • ANIMATION TEST", 34, new Vector2(0, 860), new Vector2(980, 64), new Color(0.96f, 0.78f, 0.43f));
            characterNameText = CreateLabel(safe, font, "CharacterNameText", "Character Name", 30, new Vector2(0, 720), new Vector2(920, 56), Color.white);
            characterMetaText = CreateLabel(safe, font, "CharacterMetaText", "Category | Move | Attack", 22, new Vector2(0, 665), new Vector2(920, 44), new Color(0.75f, 0.82f, 0.95f));
            currentAnimationText = CreateLabel(safe, font, "CurrentAnimationText", "Current Animation: Idle", 26, new Vector2(0, -470), new Vector2(860, 52), new Color(0.45f, 0.95f, 0.62f));

            CreateButton(safe, font, "PrevCharacterButton", "< PREV", new Vector2(-370, 785), new Vector2(190, 64), new Color(0.22f, 0.25f, 0.35f), 22, SelectPreviousCharacter);
            CreateButton(safe, font, "NextCharacterButton", "NEXT >", new Vector2(370, 785), new Vector2(190, 64), new Color(0.22f, 0.25f, 0.35f), 22, SelectNextCharacter);

            characterDropdown = CreateSimpleDropdown(safe, font, "CharacterDropdown", new Vector2(0, 785), new Vector2(500, 64));
            characterDropdown.onValueChanged.AddListener(SelectCharacter);

            CreateButton(safe, font, "IdleButton", "IDLE", new Vector2(-330, -575), new Vector2(200, 76), new Color(0.20f, 0.32f, 0.44f), 24, PlayIdle);
            CreateButton(safe, font, "WalkButton", "WALK", new Vector2(-110, -575), new Vector2(200, 76), new Color(0.20f, 0.38f, 0.30f), 24, PlayWalk);
            CreateButton(safe, font, "AttackButton", "ATTACK", new Vector2(110, -575), new Vector2(200, 76), new Color(0.48f, 0.22f, 0.18f), 24, PlayAttack);
            CreateButton(safe, font, "HitButton", "HIT", new Vector2(330, -575), new Vector2(200, 76), new Color(0.48f, 0.36f, 0.16f), 24, PlayHit);
            CreateButton(safe, font, "DeathButton", "DEATH", new Vector2(-170, -675), new Vector2(300, 78), new Color(0.35f, 0.16f, 0.28f), 24, PlayDeath);
            CreateButton(safe, font, "PlayAllButton", "PLAY ALL (5 CLIPS)", new Vector2(170, -675), new Vector2(340, 78), new Color(0.44f, 0.28f, 0.12f), 24, PlayAllSequence);
        }

        private void PopulateCharacters()
        {
            if (database == null || characterDropdown == null) return;
            characterDropdown.ClearOptions();
            var options = new List<Dropdown.OptionData>();
            for (int i = 0; i < database.Entries.Count; i++)
            {
                var entry = database.Entries[i];
                if (entry == null) continue;
                options.Add(new Dropdown.OptionData($"{i + 1:00}. {entry.displayName} ({entry.characterType})"));
            }
            characterDropdown.AddOptions(options);
        }

        public void SelectPreviousCharacter()
        {
            if (database == null || database.Entries.Count == 0) return;
            int next = (currentIndex - 1 + database.Entries.Count) % database.Entries.Count;
            SelectCharacter(next);
        }

        public void SelectNextCharacter()
        {
            if (database == null || database.Entries.Count == 0) return;
            int next = (currentIndex + 1) % database.Entries.Count;
            SelectCharacter(next);
        }

        public void SelectCharacter(int index)
        {
            if (database == null || database.Entries.Count == 0) return;
            currentIndex = Mathf.Clamp(index, 0, database.Entries.Count - 1);

            if (characterDropdown != null && characterDropdown.value != currentIndex)
            {
                characterDropdown.SetValueWithoutNotify(currentIndex);
            }

            if (playAllCoroutine != null)
            {
                StopCoroutine(playAllCoroutine);
                playAllCoroutine = null;
            }

            if (currentInstance != null)
            {
                Destroy(currentInstance);
            }

            CharacterAnimationEntry entry = database.Entries[currentIndex];
            if (entry == null) return;

            if (entry.prefab != null)
            {
                currentInstance = Instantiate(entry.prefab, previewSpawnRoot.position, Quaternion.identity, previewSpawnRoot);
                DisableGameplayBehavioursForPreview(currentInstance);
            }
            else
            {
                currentInstance = new GameObject(entry.characterId);
                currentInstance.transform.SetParent(previewSpawnRoot, false);
                currentRenderer = currentInstance.AddComponent<SpriteRenderer>();
                currentRenderer.sprite = entry.idleSprite;
                currentAnimator = currentInstance.AddComponent<Animator>();
                currentAnimator.runtimeAnimatorController = entry.animatorController;
            }

            currentInstance.transform.localPosition = Vector3.zero;
            float previewScale = entry.characterType == CharacterCategory.Boss ? 0.85f : 1.15f;
            currentInstance.transform.localScale = Vector3.one * previewScale;

            currentAnimator = currentInstance.GetComponentInChildren<Animator>();
            currentRenderer = currentInstance.GetComponentInChildren<SpriteRenderer>();
            if (currentRenderer != null)
            {
                currentRenderer.color = Color.white;
                currentRenderer.sortingOrder = 10;
            }

            if (characterNameText != null)
            {
                characterNameText.text = $"{entry.displayName}  [{entry.characterId}]";
            }
            if (characterMetaText != null)
            {
                characterMetaText.text = $"Role: {entry.characterType}   •   Move: {entry.movementType}   •   Attack: {entry.attackType}   •   Gameplay ID: {entry.gameplayEntityId}";
            }

            PlayIdle();
        }

        private static void DisableGameplayBehavioursForPreview(GameObject go)
        {
            if (go == null) return;
            foreach (MonoBehaviour mb in go.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb != null) mb.enabled = false;
            }
        }

        public void PlayIdle()
        {
            StopPlayAllIfRunning();
            SetAnimationState("Idle");
        }

        public void PlayWalk()
        {
            StopPlayAllIfRunning();
            SetAnimationState("Walk");
        }

        public void PlayAttack()
        {
            StopPlayAllIfRunning();
            SetAnimationState("Attack");
        }

        public void PlayHit()
        {
            StopPlayAllIfRunning();
            SetAnimationState("Hit");
        }

        public void PlayDeath()
        {
            StopPlayAllIfRunning();
            SetAnimationState("Death");
        }

        public void PlayAllSequence()
        {
            StopPlayAllIfRunning();
            playAllCoroutine = StartCoroutine(PlayAllRoutine());
        }

        private void StopPlayAllIfRunning()
        {
            if (playAllCoroutine != null)
            {
                StopCoroutine(playAllCoroutine);
                playAllCoroutine = null;
            }
        }

        private IEnumerator PlayAllRoutine()
        {
            SetAnimationState("Idle");
            yield return new WaitForSeconds(1.0f);

            SetAnimationState("Walk");
            yield return new WaitForSeconds(1.0f);

            SetAnimationState("Attack");
            yield return new WaitForSeconds(0.85f);

            SetAnimationState("Hit");
            yield return new WaitForSeconds(0.6f);

            SetAnimationState("Death");
            yield return new WaitForSeconds(1.0f);

            playAllCoroutine = null;
        }

        private void SetAnimationState(string stateName)
        {
            if (currentAnimator != null && currentAnimator.runtimeAnimatorController != null)
            {
                currentAnimator.ResetTrigger("Attack");
                currentAnimator.ResetTrigger("Hit");
                currentAnimator.ResetTrigger("Die");

                switch (stateName)
                {
                    case "Idle":
                        currentAnimator.SetBool("IsMoving", false);
                        currentAnimator.Play("Idle", 0, 0f);
                        break;
                    case "Walk":
                        currentAnimator.SetBool("IsMoving", true);
                        currentAnimator.Play("Walk", 0, 0f);
                        break;
                    case "Attack":
                        currentAnimator.SetBool("IsMoving", false);
                        currentAnimator.SetTrigger("Attack");
                        break;
                    case "Hit":
                        currentAnimator.SetBool("IsMoving", false);
                        currentAnimator.SetTrigger("Hit");
                        break;
                    case "Death":
                        currentAnimator.SetBool("IsMoving", false);
                        currentAnimator.SetTrigger("Die");
                        break;
                }
            }

            if (currentAnimationText != null)
            {
                currentAnimationText.text = $"Current Animation: {stateName.ToUpperInvariant()}";
            }
        }

        private static Text CreateLabel(Transform parent, Font font, string name, string text, int fontSize, Vector2 pos, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            Text t = go.GetComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.fontStyle = FontStyle.Bold;
            return t;
        }

        private static Button CreateButton(Transform parent, Font font, string name, string label, Vector2 pos, Vector2 size, Color bgColor, int fontSize, UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            Image img = go.GetComponent<Image>();
            img.color = bgColor;
            Button btn = go.GetComponent<Button>();
            btn.onClick.AddListener(onClick);
            CreateLabel(rt, font, name + "Label", label, fontSize, Vector2.zero, size - new Vector2(16, 8), Color.white);
            return btn;
        }

        private static Dropdown CreateSimpleDropdown(Transform parent, Font font, string name, Vector2 pos, Vector2 size)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Dropdown));
            root.transform.SetParent(parent, false);
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            Image bg = root.GetComponent<Image>();
            bg.color = new Color(0.16f, 0.18f, 0.26f, 1f);

            Text label = CreateLabel(rt, font, "Label", "Select Character", 20, Vector2.zero, size - new Vector2(30, 10), Color.white);

            GameObject template = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            template.transform.SetParent(rt, false);
            RectTransform trt = template.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0);
            trt.anchorMax = new Vector2(1, 0);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0, -2);
            trt.sizeDelta = new Vector2(0, 420);
            template.GetComponent<Image>().color = new Color(0.12f, 0.13f, 0.19f, 0.98f);

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
            viewport.transform.SetParent(trt, false);
            RectTransform vprt = viewport.GetComponent<RectTransform>();
            vprt.anchorMin = Vector2.zero;
            vprt.anchorMax = Vector2.one;
            vprt.sizeDelta = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(1, 1, 1, 0.05f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(vprt, false);
            RectTransform crt = content.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 1);
            crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1);
            crt.sizeDelta = new Vector2(0, 56);

            GameObject item = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
            item.transform.SetParent(crt, false);
            RectTransform irt = item.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0, 0.5f);
            irt.anchorMax = new Vector2(1, 0.5f);
            irt.sizeDelta = new Vector2(0, 52);

            GameObject itemBg = new GameObject("Item Background", typeof(RectTransform), typeof(Image));
            itemBg.transform.SetParent(irt, false);
            RectTransform ibgrt = itemBg.GetComponent<RectTransform>();
            ibgrt.anchorMin = Vector2.zero;
            ibgrt.anchorMax = Vector2.one;
            ibgrt.sizeDelta = Vector2.zero;
            itemBg.GetComponent<Image>().color = new Color(0.20f, 0.22f, 0.30f, 1f);

            Text itemLabel = CreateLabel(irt, font, "Item Label", "Option", 18, Vector2.zero, new Vector2(460, 46), Color.white);

            Toggle toggle = item.GetComponent<Toggle>();
            toggle.targetGraphic = itemBg.GetComponent<Image>();

            ScrollRect sr = template.GetComponent<ScrollRect>();
            sr.content = crt;
            sr.viewport = vprt;
            sr.horizontal = false;
            sr.vertical = true;

            Dropdown dd = root.GetComponent<Dropdown>();
            dd.targetGraphic = bg;
            dd.template = trt;
            dd.captionText = label;
            dd.itemText = itemLabel;
            template.SetActive(false);

            return dd;
        }
    }
}
