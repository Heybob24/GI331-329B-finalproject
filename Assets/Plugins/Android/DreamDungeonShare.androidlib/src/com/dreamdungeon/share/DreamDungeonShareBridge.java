package com.dreamdungeon.share;

import android.app.Activity;
import android.content.ClipData;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.content.pm.ResolveInfo;
import android.net.Uri;
import android.util.Log;

import java.io.File;
import java.util.List;

/**
 * Android Native Share bridge using ACTION_SEND + DreamDungeonFileProvider.
 * Opens the system Android Share Sheet with the real PNG image attached.
 */
public final class DreamDungeonShareBridge {

    private static final String TAG = "DreamDungeonShare";

    private DreamDungeonShareBridge() {}

    public static boolean shareImage(
        final Activity activity,
        final String filePath,
        final String authority,
        final String chooserTitle,
        final String subject,
        final String text
    ) {
        if (activity == null) {
            Log.e(TAG, "shareImage failed: Activity is null.");
            return false;
        }
        if (filePath == null || filePath.trim().isEmpty()) {
            Log.e(TAG, "shareImage failed: filePath is empty.");
            return false;
        }

        final File imageFile = new File(filePath);
        if (!imageFile.exists() || !imageFile.isFile() || imageFile.length() <= 0) {
            Log.e(TAG, "shareImage failed: image file does not exist or is empty at " + filePath);
            return false;
        }

        final String resolvedAuthority = (authority != null && !authority.trim().isEmpty())
            ? authority
            : (activity.getPackageName() + ".dreamdungeon.fileprovider");

        final Uri contentUri;
        try {
            contentUri = DreamDungeonFileProvider.getUriForFile(activity, resolvedAuthority, imageFile);
        } catch (Exception e) {
            Log.e(TAG, "shareImage failed: could not build content URI: " + e.getMessage(), e);
            return false;
        }

        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                try {
                    Intent sendIntent = new Intent(Intent.ACTION_SEND);
                    sendIntent.setType("image/png");
                    sendIntent.putExtra(Intent.EXTRA_STREAM, contentUri);

                    if (subject != null && !subject.isEmpty()) {
                        sendIntent.putExtra(Intent.EXTRA_SUBJECT, subject);
                    }
                    if (text != null && !text.isEmpty()) {
                        sendIntent.putExtra(Intent.EXTRA_TEXT, text);
                    }

                    // ClipData ensures Android 10+ Share Sheet displays thumbnail & grants read permission
                    sendIntent.setClipData(ClipData.newRawUri("DREAM DUNGEON Screenshot", contentUri));
                    sendIntent.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);

                    String title = (chooserTitle != null && !chooserTitle.isEmpty())
                        ? chooserTitle
                        : "Share Screenshot";

                    Intent chooser = Intent.createChooser(sendIntent, title);
                    chooser.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);

                    // Grant URI permission to resolved activities for broader compatibility (Android 8-10)
                    PackageManager pm = activity.getPackageManager();
                    List<ResolveInfo> resInfoList = pm.queryIntentActivities(sendIntent, PackageManager.MATCH_DEFAULT_ONLY);
                    if (resInfoList != null) {
                        for (ResolveInfo resolveInfo : resInfoList) {
                            if (resolveInfo.activityInfo != null) {
                                String packageName = resolveInfo.activityInfo.packageName;
                                activity.grantUriPermission(packageName, contentUri, Intent.FLAG_GRANT_READ_URI_PERMISSION);
                            }
                        }
                    }

                    activity.startActivity(chooser);
                    Log.i(TAG, "Opened Android Native Share Sheet for: " + contentUri);
                } catch (Exception e) {
                    Log.e(TAG, "Failed to open Android Native Share Sheet: " + e.getMessage(), e);
                }
            }
        });

        return true;
    }
}
