package com.zlinks.package_system.service.impl;

import cn.hutool.core.io.FileUtil;
import cn.hutool.core.io.resource.ResourceUtil;
import cn.hutool.core.util.StrUtil;
import com.zlinks.package_system.service.WhitePackService;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;

import java.io.File;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

@Slf4j
@Service
public class WhitePackServiceImpl implements WhitePackService {

    private static final String TEMPLATE_SDK_UTILS = "white-pack/SDKUtils.java";
    private static final String TEMPLATE_KEYSTORE = "white-pack/my.keystore.jks";

    @Override
    public List<Map<String, String>> preview(String projectPath) {
        List<Map<String, String>> changes = new ArrayList<>();
        applyInternal(projectPath, true, changes);
        return changes;
    }

    @Override
    public List<Map<String, String>> apply(String projectPath) {
        List<Map<String, String>> changes = new ArrayList<>();
        applyInternal(projectPath, false, changes);
        return changes;
    }

    private void applyInternal(String projectPath, boolean dryRun, List<Map<String, String>> changes) {
        File baseDir = new File(projectPath);
        if (!baseDir.exists() || !baseDir.isDirectory()) {
            change(changes, dryRun, "VALIDATE", "error", "项目路径不存在: " + projectPath);
            return;
        }

        // --- 1. app/build.gradle ---
        processBuildGradle(projectPath, dryRun, changes);

        // --- 2. AndroidManifest.xml ---
        processAndroidManifest(projectPath, dryRun, changes);

        // --- 3. App.java ---
        processAppJava(projectPath, dryRun, changes);

        // --- 4. MainActivity.java ---
        processMainActivityJava(projectPath, dryRun, changes);

        // --- 5. SDKUtils.java (copy template) ---
        processSDKUtils(projectPath, dryRun, changes);

        // --- 6. gradle.properties ---
        processGradleProperties(projectPath, dryRun, changes);

        // --- 7. my.keystore.jks (copy template) ---
        processKeystore(projectPath, dryRun, changes);
    }

    // ==================== app/build.gradle ====================

    private void processBuildGradle(String projectPath, boolean dryRun, List<Map<String, String>> changes) {
        File file = new File(projectPath, "app/build.gradle");
        if (!file.exists()) {
            change(changes, dryRun, "app/build.gradle", "skip", "文件不存在");
            return;
        }
        String content = FileUtil.readString(file, StandardCharsets.UTF_8);
        String original = content;

        // 1) implementation fileTree: add *.aar (only implementation line)
        if (!content.contains("*.aar")) {
            String[] lines = content.split("\n");
            for (int i = 0; i < lines.length; i++) {
                String line = lines[i];
                if (line.contains("implementation") && line.contains("fileTree") && line.contains("*.jar") && !line.contains("*.aar")) {
                    lines[i] = line.replace("['*.jar']", "['*.jar','*.aar']");
                    break;
                }
            }
            content = String.join("\n", lines);
        }

        // 2) buildToolsVersion
        content = content.replaceAll("buildToolsVersion '\\S+'", "buildToolsVersion '30.0.3'");

        // 3) Add incremental = true before the closing } of compileOptions
        content = content.replaceAll(
                "(targetCompatibility\\s+JavaVersion\\.VERSION_1_\\d+)(\\s*\\n\\s*)}(?=\\s*\\n)",
                "$1$2    incremental = true$2}");

        // 4) versionCode -> 100 (only non-commented lines)
        content = content.replaceAll("(?m)^\\s*versionCode\\s+\\d+", "        versionCode 100");

        // 5) versionName -> '1.0.0' (only non-commented lines)
        content = content.replaceAll("(?m)^\\s*versionName '\\S+'", "        versionName '1.0.0'");

        // 6) Insert signingConfigs after lintOptions block
        if (!content.contains("signingConfigs {")) {
            String lintEnd = "(abortOnError\\s+false\\s*\\n\\s*})";
            String signingBlock = "    signingConfigs {\n" +
                    "        debug {\n" +
                    "            storeFile file(\"..\\\\my.keystore.jks\")\n" +
                    "            storePassword \"Ab123145\"\n" +
                    "            keyAlias \"jy\"\n" +
                    "            keyPassword \"Ab123145\"\n" +
                    "        }\n" +
                    "        release {\n" +
                    "            storeFile file(\"..\\\\my.keystore.jks\")\n" +
                    "            storePassword \"Ab123145\"\n" +
                    "            keyAlias \"jy\"\n" +
                    "            keyPassword \"Ab123145\"\n" +
                    "        }\n" +
                    "    }\n";
            content = content.replaceAll(lintEnd, "$1\n\n" + signingBlock);
        }

        // 7) release signingConfig -> release (only in release block)
        content = content.replaceAll(
                "(release\\s*\\{[^}]*signingConfig\\s+signingConfigs)\\.debug",
                "$1.release");

        if (!content.equals(original)) {
            if (!dryRun) {
                FileUtil.writeString(content, file, StandardCharsets.UTF_8);
            }
            change(changes, dryRun, "app/build.gradle", "modified", "buildToolsVersion/version/signing等配置已更新");
        } else {
            change(changes, dryRun, "app/build.gradle", "unchanged", "无需修改或已处理过");
        }
    }

    // ==================== AndroidManifest.xml ====================

    private void processAndroidManifest(String projectPath, boolean dryRun, List<Map<String, String>> changes) {
        File file = new File(projectPath, "app/src/main/AndroidManifest.xml");
        if (!file.exists()) {
            change(changes, dryRun, "AndroidManifest.xml", "skip", "文件不存在");
            return;
        }
        String content = FileUtil.readString(file, StandardCharsets.UTF_8);
        String original = content;

        // 1) Add tools namespace
        if (!content.contains("xmlns:tools")) {
            content = content.replaceFirst(
                    "xmlns:android=\"http://schemas.android.com/apk/res/android\"",
                    "xmlns:android=\"http://schemas.android.com/apk/res/android\"\n    xmlns:tools=\"http://schemas.android.com/tools\"");
        }

        // 2) allowBackup true -> false
        content = content.replace("android:allowBackup=\"true\"", "android:allowBackup=\"false\"");

        // 3) Add tools:replace after allowBackup change
        if (!content.contains("tools:replace=\"android:allowBackup\"")) {
            content = content.replaceFirst(
                    "android:allowBackup=\"false\"",
                    "android:allowBackup=\"false\"\n        tools:replace=\"android:allowBackup\"");
        }

        // 4) launchMode singleTask -> singleTop
        content = content.replace("android:launchMode=\"singleTask\"", "android:launchMode=\"singleTop\"");

        if (!content.equals(original)) {
            if (!dryRun) {
                FileUtil.writeString(content, file, StandardCharsets.UTF_8);
            }
            change(changes, dryRun, "AndroidManifest.xml", "modified", "tools命名空间/allowBackup/launchMode已更新");
        } else {
            change(changes, dryRun, "AndroidManifest.xml", "unchanged", "无需修改或已处理过");
        }
    }

    // ==================== App.java ====================

    private void processAppJava(String projectPath, boolean dryRun, List<Map<String, String>> changes) {
        File file = new File(projectPath, "app/src/main/java/com/android/boot/App.java");
        if (!file.exists()) {
            change(changes, dryRun, "App.java", "skip", "文件不存在");
            return;
        }
        String content = FileUtil.readString(file, StandardCharsets.UTF_8);
        String original = content;

        // 1) Add SDKUtils import
        if (!content.contains("import com.android.common.SDKUtils;")) {
            content = content.replaceFirst(
                    "import android\\.content\\.Context;",
                    "import android.content.Context;\n\nimport com.android.common.SDKUtils;");
        }

        // 2) Add SDKUtils.init(this) in onCreate (preserve whitespace)
        if (!content.contains("SDKUtils.init(this);")) {
            content = content.replaceAll(
                    "(super\\.onCreate\\(\\);)(\\s*)",
                    "$1\n        SDKUtils.init(this);$2");
        }

        if (!content.equals(original)) {
            if (!dryRun) {
                FileUtil.writeString(content, file, StandardCharsets.UTF_8);
            }
            change(changes, dryRun, "App.java", "modified", "SDKUtils导入及初始化已添加");
        } else {
            change(changes, dryRun, "App.java", "unchanged", "无需修改或已处理过");
        }
    }

    // ==================== MainActivity.java ====================

    private void processMainActivityJava(String projectPath, boolean dryRun, List<Map<String, String>> changes) {
        File file = new File(projectPath, "app/src/main/java/com/android/boot/MainActivity.java");
        if (!file.exists()) {
            change(changes, dryRun, "MainActivity.java", "skip", "文件不存在");
            return;
        }
        String content = FileUtil.readString(file, StandardCharsets.UTF_8);
        String original = content;

        // 1) Add new imports
        if (!content.contains("import android.util.Log;")) {
            content = content.replaceFirst(
                    "import android\\.view\\.Window;",
                    "import android.util.Log;\nimport android.view.KeyEvent;\nimport android.view.Window;\nimport android.widget.Toast;");
        }

        // 2) Add SDKUtils import (after android.* imports)
        if (!content.contains("import com.android.common.SDKUtils;")) {
            content = content.replaceFirst(
                    "(\nimport android\\.[^;]+;)(\\s*\nimport java)",
                    "$1\nimport com.android.common.SDKUtils;$2");
        }

        // 3) Add SDKUtils.onCreate(this) in onCreate (preserve whitespace)
        if (!content.contains("SDKUtils.onCreate(this);")) {
            content = content.replaceFirst(
                    "(instance\\s*=\\s*this;)(\\s*)",
                    "$1\n        SDKUtils.onCreate(this);$2");
        }

        // 4) Add onKeyDown method after onJniCall
        if (!content.contains("public boolean onKeyDown")) {
            String onJniCallEnd = CaptureUtils.captureBlock(content, "public void onJniCall");
            if (onJniCallEnd != null) {
                String keyDownMethod = "\n    @Override\n" +
                        "    public boolean onKeyDown(int keyCode, KeyEvent event) {\n" +
                        "        if (SDKUtils.onKeyDown(this, keyCode, event)) {\n" +
                        "            return true;\n" +
                        "        }\n" +
                        "        return super.onKeyDown(keyCode, event);\n" +
                        "    }\n";
                content = content.replace(onJniCallEnd, onJniCallEnd + keyDownMethod);
            }
        }

        // 5) Add onRestart method after onKeyDown
        if (!content.contains("SDKUtils.onRestart(this);")) {
            String keyDownEnd = CaptureUtils.captureBlock(content, "public boolean onKeyDown");
            if (keyDownEnd != null) {
                String restartMethod = "\n    @Override\n" +
                        "    protected void onRestart() {\n" +
                        "        super.onRestart();\n" +
                        "        SDKUtils.onRestart(this);\n" +
                        "    }\n";
                content = content.replace(keyDownEnd, keyDownEnd + restartMethod);
            }
        }

        // 6) Modify onPause
        if (!content.contains("SDKUtils.onPause(this);")) {
            content = content.replaceFirst(
                    "(protected void onPause\\(\\)\\s*\\{[^}]*super\\.onPause\\(\\);)\\s*\\}",
                    "$1\n        SDKUtils.onPause(this);\n    }");
        }

        // 7) Modify onResume
        if (!content.contains("SDKUtils.onResume(this);")) {
            content = content.replaceFirst(
                    "(protected void onResume\\(\\)\\s*\\{[^}]*super\\.onResume\\(\\);)\\s*\\}",
                    "$1\n        SDKUtils.onResume(this);\n    }");
        }

        // 8) Replace showVideo case in callJava
        if (!content.contains("SDKUtils.showRewardedVideo")) {
            String videoReplacement =
                    "case \"showVideo\":\n" +
                    "                Log.e(\"Zlinks\",\"callJava-showRewardedVideo\");\n" +
                    "                SDKUtils.showRewardedVideo(new SDKUtils.IRewardVideoListener() {\n" +
                    "                    @Override\n" +
                    "                    public void onRewardLoaded() {\n" +
                    "                        Log.e(\"Zlinks\",\"onRewardLoaded\");\n" +
                    "                    }\n" +
                    "\n" +
                    "                    @Override\n" +
                    "                    public void onRewardLoadedFail() {\n" +
                    "                        Log.e(\"Zlinks\",\"onRewardLoadedFail\");\n" +
                    "                        Toast.makeText(MainActivity.mActivity, \"广告未准备好, 请稍后再试\", Toast.LENGTH_SHORT).show();\n" +
                    "                    }\n" +
                    "\n" +
                    "                    @Override\n" +
                    "                    public void onReward() {\n" +
                    "                        Log.e(\"Zlinks\",\"onReward\");\n" +
                    "                        showVideo();\n" +
                    "                    }\n" +
                    "\n" +
                    "                    @Override\n" +
                    "                    public void onRewardHidden() {\n" +
                    "                        Log.e(\"Zlinks\",\"onRewardHidden\");\n" +
                    "                    }\n" +
                    "\n" +
                    "                    @Override\n" +
                    "                    public void onRewardClicked() {\n" +
                    "                        Log.e(\"Zlinks\",\"onRewardClicked\");\n" +
                    "                    }\n" +
                    "                });\n" +
                    "                break;";

            content = content.replaceFirst(
                    "case \"showVideo\":\\s*\\n\\s*showVideo\\(\\);\\s*\\n\\s*break;",
                    Matcher.quoteReplacement(videoReplacement));
        }

        if (!content.equals(original)) {
            if (!dryRun) {
                FileUtil.writeString(content, file, StandardCharsets.UTF_8);
            }
            change(changes, dryRun, "MainActivity.java", "modified", "SDK广告生命周期及激励视频回调已添加");
        } else {
            change(changes, dryRun, "MainActivity.java", "unchanged", "无需修改或已处理过");
        }
    }

    // ==================== SDKUtils.java (copy from template) ====================

    private void processSDKUtils(String projectPath, boolean dryRun, List<Map<String, String>> changes) {
        File targetDir = new File(projectPath, "app/src/main/java/com/android/common");
        File targetFile = new File(targetDir, "SDKUtils.java");
        if (targetFile.exists()) {
            change(changes, dryRun, "SDKUtils.java", "unchanged", "文件已存在");
            return;
        }
        try {
            byte[] templateBytes = ResourceUtil.readBytes(TEMPLATE_SDK_UTILS);
            if (templateBytes != null && templateBytes.length > 0) {
                if (!dryRun) {
                    FileUtil.mkdir(targetDir);
                    FileUtil.writeBytes(templateBytes, targetFile);
                }
                change(changes, dryRun, "SDKUtils.java", "created", "SDK工具类已添加");
            } else {
                change(changes, dryRun, "SDKUtils.java", "error", "模板文件为空");
            }
        } catch (Exception e) {
            change(changes, dryRun, "SDKUtils.java", "error", "模板复制失败: " + e.getMessage());
        }
    }

    // ==================== gradle.properties ====================

    private void processGradleProperties(String projectPath, boolean dryRun, List<Map<String, String>> changes) {
        File file = new File(projectPath, "gradle.properties");
        if (!file.exists()) {
            change(changes, dryRun, "gradle.properties", "skip", "文件不存在");
            return;
        }
        String content = FileUtil.readString(file, StandardCharsets.UTF_8);
        String original = content;

        // Ensure android.injected.testOnly=false
        if (!content.contains("android.injected.testOnly=false")) {
            content = "android.injected.testOnly=false\n" + content;
        }

        // Replace jvmargs line
        if (content.contains("org.gradle.jvmargs=")) {
            String newJvmArgs = "org.gradle.jvmargs=-Xmx4096M -XX:+HeapDumpOnOutOfMemoryError -Dfile.encoding=UTF-8";
            content = content.replaceAll("org\\.gradle\\.jvmargs=.*", Matcher.quoteReplacement(newJvmArgs));
        } else {
            content += "\norg.gradle.jvmargs=-Xmx4096M -XX:+HeapDumpOnOutOfMemoryError -Dfile.encoding=UTF-8";
        }

        // Ensure parallel, daemon, caching
        if (!content.contains("org.gradle.parallel=")) {
            content += "\norg.gradle.parallel=true";
        }
        if (!content.contains("org.gradle.daemon=")) {
            content += "\norg.gradle.daemon=true";
        }
        if (!content.contains("org.gradle.caching=")) {
            content += "\norg.gradle.caching=true";
        }

        // Remove duplicate empty lines
        content = content.replaceAll("\n{3,}", "\n\n");

        if (!content.equals(original)) {
            if (!dryRun) {
                FileUtil.writeString(content, file, StandardCharsets.UTF_8);
            }
            change(changes, dryRun, "gradle.properties", "modified", "Gradle构建属性已更新");
        } else {
            change(changes, dryRun, "gradle.properties", "unchanged", "无需修改或已处理过");
        }
    }

    // ==================== my.keystore.jks (copy from template) ====================

    private void processKeystore(String projectPath, boolean dryRun, List<Map<String, String>> changes) {
        File targetFile = new File(projectPath, "my.keystore.jks");
        if (targetFile.exists()) {
            change(changes, dryRun, "my.keystore.jks", "unchanged", "文件已存在");
            return;
        }
        try {
            byte[] templateBytes = ResourceUtil.readBytes(TEMPLATE_KEYSTORE);
            if (templateBytes != null && templateBytes.length > 0) {
                if (!dryRun) {
                    FileUtil.writeBytes(templateBytes, targetFile);
                }
                change(changes, dryRun, "my.keystore.jks", "created", "签名密钥已添加");
            } else {
                change(changes, dryRun, "my.keystore.jks", "error", "模板文件为空");
            }
        } catch (Exception e) {
            change(changes, dryRun, "my.keystore.jks", "error", "模板复制失败: " + e.getMessage());
        }
    }

    // ==================== Helper ====================

    private void change(List<Map<String, String>> changes, boolean dryRun,
                        String file, String status, String message) {
        Map<String, String> entry = new LinkedHashMap<>();
        entry.put("file", file);
        entry.put("status", status);
        entry.put("message", message);
        if (dryRun) {
            entry.put("dryRun", "true");
        }
        changes.add(entry);
    }

    /**
     * Captures a complete method/block from its opening brace to its closing brace.
     */
    private static class CaptureUtils {
        static String captureBlock(String content, String signaturePattern) {
            Pattern p = Pattern.compile(Pattern.quote(signaturePattern));
            Matcher m = p.matcher(content);
            if (!m.find()) return null;

            int start = m.end();
            int braceStart = content.indexOf('{', start);
            if (braceStart == -1) return null;

            int depth = 0;
            int i = braceStart;
            while (i < content.length()) {
                char c = content.charAt(i);
                if (c == '{') depth++;
                else if (c == '}') {
                    depth--;
                    if (depth == 0) {
                        return content.substring(m.start(), i + 1);
                    }
                }
                i++;
            }
            return null;
        }
    }
}
