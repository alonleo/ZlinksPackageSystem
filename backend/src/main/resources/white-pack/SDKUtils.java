package com.android.common;

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
