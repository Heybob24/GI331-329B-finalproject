package com.dreamdungeon.share;

import android.content.ContentProvider;
import android.content.ContentValues;
import android.content.Context;
import android.database.Cursor;
import android.database.MatrixCursor;
import android.net.Uri;
import android.os.ParcelFileDescriptor;
import android.provider.OpenableColumns;

import java.io.File;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.util.List;

/**
 * Lightweight, zero-dependency FileProvider for DREAM DUNGEON (Unity 6 Android).
 * Serves temporary screenshot files strictly from the app's cache directories
 * (externalCacheDir or internal cacheDir) for ACTION_SEND native sharing.
 */
public class DreamDungeonFileProvider extends ContentProvider {

    private static final String ROOT_EXT_CACHE = "ext_cache";
    private static final String ROOT_INT_CACHE = "int_cache";

    private static final String[] DEFAULT_COLUMNS = {
        OpenableColumns.DISPLAY_NAME,
        OpenableColumns.SIZE
    };

    public static Uri getUriForFile(Context context, String authority, File file) throws IOException {
        if (context == null || authority == null || file == null) {
            throw new IllegalArgumentException("Context, authority, and file must not be null.");
        }

        String canonicalFilePath = file.getCanonicalPath();

        File extCache = context.getExternalCacheDir();
        if (extCache != null) {
            String extRoot = extCache.getCanonicalPath();
            if (isUnderRoot(canonicalFilePath, extRoot)) {
                String relative = getRelativePath(canonicalFilePath, extRoot);
                return new Uri.Builder()
                    .scheme("content")
                    .authority(authority)
                    .appendPath(ROOT_EXT_CACHE)
                    .appendEncodedPath(Uri.encode(relative, "/"))
                    .build();
            }
        }

        File intCache = context.getCacheDir();
        if (intCache != null) {
            String intRoot = intCache.getCanonicalPath();
            if (isUnderRoot(canonicalFilePath, intRoot)) {
                String relative = getRelativePath(canonicalFilePath, intRoot);
                return new Uri.Builder()
                    .scheme("content")
                    .authority(authority)
                    .appendPath(ROOT_INT_CACHE)
                    .appendEncodedPath(Uri.encode(relative, "/"))
                    .build();
            }
        }

        throw new IllegalArgumentException("File is outside allowed cache directories: " + canonicalFilePath);
    }

    private static boolean isUnderRoot(String filePath, String rootPath) {
        if (rootPath.endsWith(File.separator)) {
            return filePath.startsWith(rootPath);
        }
        return filePath.startsWith(rootPath + File.separator);
    }

    private static String getRelativePath(String filePath, String rootPath) {
        int startIndex = rootPath.endsWith(File.separator) ? rootPath.length() : rootPath.length() + 1;
        return filePath.substring(startIndex).replace(File.separatorChar, '/');
    }

    private File resolveFile(Uri uri) throws FileNotFoundException {
        Context context = getContext();
        if (context == null || uri == null) {
            throw new FileNotFoundException("Invalid context or URI.");
        }

        List<String> segments = uri.getPathSegments();
        if (segments == null || segments.size() < 2) {
            throw new FileNotFoundException("Invalid content URI: " + uri);
        }

        String rootTag = segments.get(0);
        File rootDir;
        if (ROOT_EXT_CACHE.equals(rootTag)) {
            rootDir = context.getExternalCacheDir();
        } else if (ROOT_INT_CACHE.equals(rootTag)) {
            rootDir = context.getCacheDir();
        } else {
            throw new FileNotFoundException("Unsupported root segment: " + rootTag);
        }

        if (rootDir == null) {
            throw new FileNotFoundException("Cache directory unavailable.");
        }

        StringBuilder relativeBuilder = new StringBuilder();
        for (int i = 1; i < segments.size(); i++) {
            if (i > 1) relativeBuilder.append(File.separatorChar);
            relativeBuilder.append(segments.get(i));
        }

        File target = new File(rootDir, relativeBuilder.toString());
        try {
            String canonicalRoot = rootDir.getCanonicalPath();
            String canonicalTarget = target.getCanonicalPath();
            if (!isUnderRoot(canonicalTarget, canonicalRoot)) {
                throw new SecurityException("Path traversal blocked for URI: " + uri);
            }
            if (!target.exists() || !target.isFile()) {
                throw new FileNotFoundException("File not found: " + canonicalTarget);
            }
            return target;
        } catch (IOException e) {
            throw new FileNotFoundException("Failed to resolve canonical path: " + e.getMessage());
        }
    }

    @Override
    public boolean onCreate() {
        return true;
    }

    @Override
    public String getType(Uri uri) {
        String path = uri != null ? uri.getLastPathSegment() : null;
        if (path != null) {
            String lower = path.toLowerCase();
            if (lower.endsWith(".png")) return "image/png";
            if (lower.endsWith(".jpg") || lower.endsWith(".jpeg")) return "image/jpeg";
        }
        return "application/octet-stream";
    }

    @Override
    public Cursor query(Uri uri, String[] projection, String selection, String[] selectionArgs, String sortOrder) {
        File file;
        try {
            file = resolveFile(uri);
        } catch (FileNotFoundException e) {
            return new MatrixCursor(DEFAULT_COLUMNS, 0);
        }

        String[] cols = (projection != null && projection.length > 0) ? projection : DEFAULT_COLUMNS;
        String[] outCols = new String[cols.length];
        Object[] outVals = new Object[cols.length];
        int count = 0;

        for (String col : cols) {
            if (OpenableColumns.DISPLAY_NAME.equals(col)) {
                outCols[count] = OpenableColumns.DISPLAY_NAME;
                outVals[count] = file.getName();
                count++;
            } else if (OpenableColumns.SIZE.equals(col)) {
                outCols[count] = OpenableColumns.SIZE;
                outVals[count] = file.length();
                count++;
            }
        }

        String[] finalCols = new String[count];
        Object[] finalVals = new Object[count];
        System.arraycopy(outCols, 0, finalCols, 0, count);
        System.arraycopy(outVals, 0, finalVals, 0, count);

        MatrixCursor cursor = new MatrixCursor(finalCols, 1);
        cursor.addRow(finalVals);
        return cursor;
    }

    @Override
    public ParcelFileDescriptor openFile(Uri uri, String mode) throws FileNotFoundException {
        if (mode != null && !mode.equals("r")) {
            throw new SecurityException("Read-only provider: mode '" + mode + "' is not allowed.");
        }
        File file = resolveFile(uri);
        return ParcelFileDescriptor.open(file, ParcelFileDescriptor.MODE_READ_ONLY);
    }

    @Override
    public Uri insert(Uri uri, ContentValues values) {
        throw new UnsupportedOperationException("Insert not supported.");
    }

    @Override
    public int delete(Uri uri, String selection, String[] selectionArgs) {
        throw new UnsupportedOperationException("Delete not supported.");
    }

    @Override
    public int update(Uri uri, ContentValues values, String selection, String[] selectionArgs) {
        throw new UnsupportedOperationException("Update not supported.");
    }
}
