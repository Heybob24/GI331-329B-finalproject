DREAM DUNGEON — Sprite Animation Pack (40 Characters x 32 Frames = 1,280 Frames)
================================================================================

โครงสร้างโฟลเดอร์ (Folder Structure):
- spritesheets/  : ไฟล์ Sprite Sheet พื้นหลังโปร่งใส (PNG) ขนาดกริด 5 แถว x 8 คอลัมน์ พร้อมไฟล์ .json ระบุพิกัดเฟรมและ FPS
- frames/        : ไฟล์ภาพแยกทีละเฟรม (PNG โปร่งใส) จัดแยกโฟลเดอร์ตามตัวละครและท่าทาง (idle, walk, attack, hit, death)
- gifs/          : ไฟล์ GIF ตัวอย่างแอนิเมชันครบทั้ง 5 ท่าทางสำหรับเปิดดูได้ทันที
- manifest.json  : ดัชนีรวมรายชื่อตัวละครทั้ง 40 ตัว ขนาดเซลล์ และพาธไฟล์ทั้งหมด

การจัดเรียงแถวใน Sprite Sheet (5 Rows x 8 Columns):
- Row 0 : Idle   (6 เฟรม, แนะนำ 8 FPS, Loop)
- Row 1 : Walk   (8 เฟรม, แนะนำ 10 FPS, Loop)
- Row 2 : Attack (8 เฟรม, แนะนำ 12 FPS, One-shot)
- Row 3 : Hit    (4 เฟรม, แนะนำ 12 FPS, One-shot)
- Row 4 : Death  (6 เฟรม, แนะนำ 8 FPS, One-shot)

หมายเหตุ:
- แยกสไปรท์ที่มี 2 ตัวละครในภาพเดียวออกเป็นตัวเดี่ยวแล้ว ได้แก่ sprite_10 (10a_dire_wolf, 10b_goblin) และ sprite_38 (38a_venom_spider, 38b_frost_spider) รวมทั้งหมด 40 ตัว
- ทำความสะอาดเศษขอบภาพที่ติดมาในสไปรท์ต้นฉบับเรียบร้อยแล้ว
