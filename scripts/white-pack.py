#!/usr/bin/env python3
"""白包处理 - 快速应用“初始工程”到“重置版本号”的修改

用法:
    python white-pack.py <project_path>           直接处理
    python white-pack.py <project_path> --preview  预览变更(不实际修改)
    python white-pack.py <project_path> --restore  恢复备份(若已处理过)

功能:
    对指定Unity+FakerAndroid游戏项目进行内容级修改，忽略格式差异。
    包含: build.gradle / AndroidManifest.xml / App.java / MainActivity.java / 
          gradle.properties 的修改，以及 SDKUtils.java / my.keystore.jks 的新增。
"""

import sys
import os
import re
import base64
import shutil
import argparse
from datetime import datetime

# ============================================================
# 模板文件（内嵌）
# ============================================================

SDK_UTILS_JAVA = r'''package com.android.common;

import android.app.Activity;
import android.app.Application;
import android.os.Handler;
import android.os.Looper;
import android.os.Message;
import android.view.KeyEvent;

public class SDKUtils {
    private static Application g_App =  null;

    public static final int MSG_REFRESH_RV = 9000;
    public static final Handler handler = new Handler(Looper.getMainLooper()) {
        @Override
        public void handleMessage(Message msg) {
            switch (msg.what) {
                case MSG_REFRESH_RV:{
                    break;
                }
            }
            super.handleMessage(msg);
        }
    };


    public interface IRewardVideoListener {
        void onRewardLoaded();
        void onRewardLoadedFail();
        void onReward();
        void onRewardHidden();
        void onRewardClicked();
    }

    public static IRewardVideoListener g_listener = null;

    public static void init(Application app) {
        g_App = app;
    }

    public static void onCreate(Activity activity) {
    }

    public static void onResume(Activity activity) {
    }

    public static void onPause(Activity activity) {
    }

    public static void onRestart(Activity activity) {
    }

    public static boolean onKeyDown(Activity activity, int keyCode, KeyEvent event) {
        return false;
    }

    public static void showRewardedVideo(IRewardVideoListener listener) {
        listener.onReward();
        listener.onRewardHidden();
    }

    public static void showAd(boolean bShow) {
    }
}
'''

KEYSTORE_B64 = (
    "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"
    "kXenyo9YuWJfItE+RQ9cnc+IkcMmmLk="
)


# ============================================================
# 文件处理函数
# ============================================================

changes = []  # 全局变更记录


def record(file_name, status, message, dry_run=False):
    entry = {"file": file_name, "status": status, "message": message}
    if dry_run:
        entry["dryRun"] = "true"
    changes.append(entry)
    symbol = "[PREVIEW]" if dry_run else "[APPLY]"
    print(f"  {symbol} {status:10s} | {file_name}: {message}")


def read_utf8(path):
    with open(path, "r", encoding="utf-8") as f:
        return f.read()


def write_utf8(path, content):
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(content)


def process_build_gradle(root, dry_run):
    fp = os.path.join(root, "app", "build.gradle")
    if not os.path.isfile(fp):
        record("app/build.gradle", "skip", "文件不存在")
        return
    content = read_utf8(fp)
    original = content

    # 1) impl fileTree add *.aar (only the implementation line)
    lines = content.split("\n")
    for i, line in enumerate(lines):
        if "implementation" in line and "fileTree" in line and "*.jar" in line and "*.aar" not in line:
            lines[i] = line.replace("['*.jar']", "['*.jar','*.aar']")
            break
    content = "\n".join(lines)

    # 2) buildToolsVersion -> 30.0.3
    content = re.sub(r"buildToolsVersion\s+'[\d.]+'", "buildToolsVersion '30.0.3'", content)

    # 3) incremental = true (insert before the closing } of compileOptions)
    content = re.sub(
        r"(targetCompatibility\s+JavaVersion\.VERSION_\d+_\d+)(\s*\n\s*)}(?=\s*\n)",
        r"\1\2    incremental = true\2}",
        content,
        count=1
    )

    # 4) versionCode -> 100 (only non-commented lines)
    content = re.sub(r"(?m)^\s*versionCode\s+\d+", "        versionCode 100", content)

    # 5) versionName -> 1.0.0 (only non-commented lines)
    content = re.sub(r"(?m)^\s*versionName\s+'[^']+'", "        versionName '1.0.0'", content)

    # 6) signingConfigs after lintOptions
    if "signingConfigs {" not in content:
        signing_block = (
            "    signingConfigs {\n"
            "        debug {\n"
            '            storeFile file("..\\\\my.keystore.jks")\n'
            '            storePassword "Ab123145"\n'
            '            keyAlias "jy"\n'
            '            keyPassword "Ab123145"\n'
            "        }\n"
            "        release {\n"
            '            storeFile file("..\\\\my.keystore.jks")\n'
            '            storePassword "Ab123145"\n'
            '            keyAlias "jy"\n'
            '            keyPassword "Ab123145"\n'
            "        }\n"
            "    }\n"
        )
        content = re.sub(
            r"(abortOnError\s+false\s*\n\s*})",
            r"\1\n\n" + signing_block,
            content
        )

    # 7) release signingConfig -> release (only in release block)
    content = re.sub(
        r"(release\s*\{[^}]*signingConfig\s+signingConfigs)\.debug",
        r"\1.release",
        content,
        flags=re.DOTALL
    )

    if content != original:
        if not dry_run:
            write_utf8(fp, content)
        record("app/build.gradle", "modified", "buildToolsVersion/version/signing已更新", dry_run)
    else:
        record("app/build.gradle", "unchanged", "无需修改或已处理过", dry_run)


def process_android_manifest(root, dry_run):
    fp = os.path.join(root, "app", "src", "main", "AndroidManifest.xml")
    if not os.path.isfile(fp):
        record("AndroidManifest.xml", "skip", "文件不存在")
        return
    content = read_utf8(fp)
    original = content

    # 1) tools namespace
    if "xmlns:tools" not in content:
        content = content.replace(
            'xmlns:android="http://schemas.android.com/apk/res/android"',
            'xmlns:android="http://schemas.android.com/apk/res/android"\n    xmlns:tools="http://schemas.android.com/tools"',
            1
        )

    # 2) allowBackup true -> false
    content = content.replace('android:allowBackup="true"', 'android:allowBackup="false"')

    # 3) tools:replace
    if 'tools:replace="android:allowBackup"' not in content:
        content = content.replace(
            'android:allowBackup="false"',
            'android:allowBackup="false"\n        tools:replace="android:allowBackup"',
            1
        )

    # 4) launchMode singleTask -> singleTop
    content = content.replace('android:launchMode="singleTask"', 'android:launchMode="singleTop"')

    if content != original:
        if not dry_run:
            write_utf8(fp, content)
        record("AndroidManifest.xml", "modified", "tools命名空间/allowBackup/launchMode已更新", dry_run)
    else:
        record("AndroidManifest.xml", "unchanged", "无需修改或已处理过", dry_run)


def process_app_java(root, dry_run):
    fp = os.path.join(root, "app", "src", "main", "java", "com", "android", "boot", "App.java")
    if not os.path.isfile(fp):
        record("App.java", "skip", "文件不存在")
        return
    content = read_utf8(fp)
    original = content

    # 1) SDKUtils import
    if "import com.android.common.SDKUtils;" not in content:
        content = re.sub(
            r"(import android\.content\.Context;)\s*",
            r"\1\n\nimport com.android.common.SDKUtils;\n",
            content,
            count=1
        )

    # 2) SDKUtils.init in onCreate (preserve whitespace before closing brace)
    if "SDKUtils.init(this);" not in content:
        content = re.sub(
            r"(super\.onCreate\(\);)(\s*)",
            r"\1\n        SDKUtils.init(this);\2",
            content,
            count=1
        )

    if content != original:
        if not dry_run:
            write_utf8(fp, content)
        record("App.java", "modified", "SDKUtils导入及初始化已添加", dry_run)
    else:
        record("App.java", "unchanged", "无需修改或已处理过", dry_run)


def process_main_activity(root, dry_run):
    fp = os.path.join(root, "app", "src", "main", "java", "com", "android", "boot", "MainActivity.java")
    if not os.path.isfile(fp):
        record("MainActivity.java", "skip", "文件不存在")
        return
    content = read_utf8(fp)
    original = content

    # 1) Add imports (Log, KeyEvent, Toast)
    if "import android.util.Log;" not in content:
        content = content.replace(
            "import android.view.Window;",
            "import android.util.Log;\nimport android.view.KeyEvent;\nimport android.view.Window;\nimport android.widget.Toast;",
            1
        )

    # 2) Add SDKUtils import (after android.* imports, before java imports)
    if "import com.android.common.SDKUtils;" not in content:
        # Insert after the last android.* import line, before java imports
        content = re.sub(
            r"(\nimport android\.[^;]+;)(\s*\nimport java)",
            r"\1\nimport com.android.common.SDKUtils;\2",
            content,
            count=1
        )

    # 3) SDKUtils.onCreate in onCreate (preserve whitespace before closing brace)
    if "SDKUtils.onCreate(this);" not in content:
        content = re.sub(
            r"(instance\s*=\s*this;)(\s*)",
            r"\1\n        SDKUtils.onCreate(this);\2",
            content,
            count=1
        )

    # 4) onKeyDown after onJniCall
    if "public boolean onKeyDown" not in content:
        on_jni_block = _capture_block(content, "public void onJniCall")
        if on_jni_block:
            key_down = (
                "\n    @Override\n"
                "    public boolean onKeyDown(int keyCode, KeyEvent event) {\n"
                "        if (SDKUtils.onKeyDown(this, keyCode, event)) {\n"
                "            return true;\n"
                "        }\n"
                "        return super.onKeyDown(keyCode, event);\n"
                "    }\n"
            )
            content = content.replace(on_jni_block, on_jni_block + key_down, 1)

    # 5) onRestart after onKeyDown
    if "SDKUtils.onRestart(this);" not in content:
        key_down_block = _capture_block(content, "public boolean onKeyDown")
        if key_down_block:
            restart_method = (
                "\n    @Override\n"
                "    protected void onRestart() {\n"
                "        super.onRestart();\n"
                "        SDKUtils.onRestart(this);\n"
                "    }\n"
            )
            content = content.replace(key_down_block, key_down_block + restart_method, 1)

    # 6) onPause
    if "SDKUtils.onPause(this);" not in content:
        content = re.sub(
            r"(protected\s+void\s+onPause\s*\(\s*\)\s*\{[^}]*super\.onPause\s*\(\s*\);)\s*\}",
            r"\1\n        SDKUtils.onPause(this);\n    }",
            content
        )

    # 7) onResume
    if "SDKUtils.onResume(this);" not in content:
        content = re.sub(
            r"(protected\s+void\s+onResume\s*\(\s*\)\s*\{[^}]*super\.onResume\s*\(\s*\);)\s*\}",
            r"\1\n        SDKUtils.onResume(this);\n    }",
            content
        )

    # 8) showVideo case replacement
    if "SDKUtils.showRewardedVideo" not in content:
        video_case_new = (
            'case "showVideo":\n'
            '                Log.e("Zlinks","callJava-showRewardedVideo");\n'
            '                SDKUtils.showRewardedVideo(new SDKUtils.IRewardVideoListener() {\n'
            '                    @Override\n'
            '                    public void onRewardLoaded() {\n'
            '                        Log.e("Zlinks","onRewardLoaded");\n'
            '                    }\n'
            '\n'
            '                    @Override\n'
            '                    public void onRewardLoadedFail() {\n'
            '                        Log.e("Zlinks","onRewardLoadedFail");\n'
            '                        Toast.makeText(MainActivity.mActivity, "广告未准备好, 请稍后再试", Toast.LENGTH_SHORT).show();\n'
            '                    }\n'
            '\n'
            '                    @Override\n'
            '                    public void onReward() {\n'
            '                        Log.e("Zlinks","onReward");\n'
            '                        showVideo();\n'
            '                    }\n'
            '\n'
            '                    @Override\n'
            '                    public void onRewardHidden() {\n'
            '                        Log.e("Zlinks","onRewardHidden");\n'
            '                    }\n'
            '\n'
            '                    @Override\n'
            '                    public void onRewardClicked() {\n'
            '                        Log.e("Zlinks","onRewardClicked");\n'
            '                    }\n'
            '                });\n'
            '                break;'
        )
        content = re.sub(
            r'case\s+"showVideo"\s*:\s*\n\s*showVideo\s*\(\s*\)\s*;\s*\n\s*break\s*;',
            video_case_new,
            content
        )

    if content != original:
        if not dry_run:
            write_utf8(fp, content)
        record("MainActivity.java", "modified", "SDK广告生命周期及激励视频回调已添加", dry_run)
    else:
        record("MainActivity.java", "unchanged", "无需修改或已处理过", dry_run)


def process_sdk_utils(root, dry_run):
    target_dir = os.path.join(root, "app", "src", "main", "java", "com", "android", "common")
    target_file = os.path.join(target_dir, "SDKUtils.java")
    if os.path.isfile(target_file):
        record("SDKUtils.java", "unchanged", "文件已存在")
        return
    if not dry_run:
        os.makedirs(target_dir, exist_ok=True)
        with open(target_file, "w", encoding="utf-8", newline="\n") as f:
            f.write(SDK_UTILS_JAVA)
    record("SDKUtils.java", "created", "SDK工具类已添加", dry_run)


def process_gradle_properties(root, dry_run):
    fp = os.path.join(root, "gradle.properties")
    if not os.path.isfile(fp):
        record("gradle.properties", "skip", "文件不存在")
        return
    content = read_utf8(fp)
    original = content

    if "android.injected.testOnly=false" not in content:
        content = "android.injected.testOnly=false\n" + content

    jvm_line = "org.gradle.jvmargs=-Xmx4096M -XX:+HeapDumpOnOutOfMemoryError -Dfile.encoding=UTF-8"
    if "org.gradle.jvmargs=" in content:
        content = re.sub(r"org\.gradle\.jvmargs=.*", jvm_line, content)
    else:
        content += "\n" + jvm_line + "\n"

    for prop, val in [("org.gradle.parallel", "true"), ("org.gradle.daemon", "true"), ("org.gradle.caching", "true")]:
        if f"{prop}=" not in content:
            content += f"{prop}={val}\n"

    content = re.sub(r"\n{3,}", "\n\n", content).rstrip() + "\n"

    if content != original:
        if not dry_run:
            write_utf8(fp, content)
        record("gradle.properties", "modified", "Gradle构建属性已更新", dry_run)
    else:
        record("gradle.properties", "unchanged", "无需修改或已处理过", dry_run)


def process_keystore(root, dry_run):
    target_file = os.path.join(root, "my.keystore.jks")
    if os.path.isfile(target_file):
        record("my.keystore.jks", "unchanged", "文件已存在")
        return
    if not dry_run:
        data = base64.b64decode(KEYSTORE_B64)
        with open(target_file, "wb") as f:
            f.write(data)
    record("my.keystore.jks", "created", "签名密钥已添加", dry_run)


def _capture_block(content, signature):
    """捕获从signature开始到匹配的}结束的代码块"""
    idx = content.find(signature)
    if idx == -1:
        return None
    start = content.find("{", idx)
    if start == -1:
        return None
    depth = 0
    for i in range(start, len(content)):
        if content[i] == "{":
            depth += 1
        elif content[i] == "}":
            depth -= 1
            if depth == 0:
                return content[idx : i + 1]
    return None


# ============================================================
# 主流程
# ============================================================

def backup_project(root):
    """创建备份目录"""
    bak_dir = os.path.join(root, f"white-pack-backup-{datetime.now().strftime('%Y%m%d_%H%M%S')}")
    if os.path.exists(bak_dir):
        return bak_dir
    os.makedirs(bak_dir)

    files_to_backup = [
        "app/build.gradle",
        "app/src/main/AndroidManifest.xml",
        "app/src/main/java/com/android/boot/App.java",
        "app/src/main/java/com/android/boot/MainActivity.java",
        "gradle.properties",
    ]
    for rel in files_to_backup:
        src = os.path.join(root, rel)
        if os.path.isfile(src):
            dst = os.path.join(bak_dir, rel.replace("/", os.sep))
            os.makedirs(os.path.dirname(dst), exist_ok=True)
            shutil.copy2(src, dst)
    return bak_dir


def restore_backup(root):
    """从最近的备份恢复"""
    import glob
    pattern = os.path.join(root, "white-pack-backup-*")
    backups = sorted(glob.glob(pattern), reverse=True)
    if not backups:
        print("未找到备份目录")
        return
    bak = backups[0]
    print(f"从备份恢复: {bak}")
    for dirpath, _, filenames in os.walk(bak):
        for fn in filenames:
            src = os.path.join(dirpath, fn)
            rel = os.path.relpath(src, bak)
            dst = os.path.join(root, rel)
            os.makedirs(os.path.dirname(dst), exist_ok=True)
            shutil.copy2(src, dst)
            print(f"  恢复: {rel}")
    shutil.rmtree(bak)
    print("恢复完成")


def main():
    parser = argparse.ArgumentParser(description="白包处理 - 快速应用内容变更", formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("project", help="游戏项目根路径")
    parser.add_argument("--preview", "-p", action="store_true", help="预览变更(dry-run)")
    parser.add_argument("--restore", "-r", action="store_true", help="从备份恢复")
    args = parser.parse_args()

    root = os.path.abspath(args.project)
    if not os.path.isdir(root):
        print(f"错误: 项目路径不存在 - {root}")
        sys.exit(1)

    if args.restore:
        restore_backup(root)
        return

    dry_run = args.preview
    mode = "预览 (Dry Run)" if dry_run else "执行"
    print(f"\n{'='*60}")
    print(f"  白包处理 - {mode}")
    print(f"  项目路径: {root}")
    print(f"{'='*60}\n")

    if not dry_run:
        bak = backup_project(root)
        print(f"  备份已创建: {bak}\n")

    process_build_gradle(root, dry_run)
    process_android_manifest(root, dry_run)
    process_app_java(root, dry_run)
    process_main_activity(root, dry_run)
    process_sdk_utils(root, dry_run)
    process_gradle_properties(root, dry_run)
    process_keystore(root, dry_run)

    print(f"\n{'='*60}")
    print(f"  处理完成, 共 {len(changes)} 项变更")
    print(f"{'='*60}\n")


if __name__ == "__main__":
    main()
