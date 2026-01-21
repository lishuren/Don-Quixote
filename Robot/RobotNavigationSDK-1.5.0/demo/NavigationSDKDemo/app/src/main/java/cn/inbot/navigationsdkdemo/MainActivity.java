package cn.inbot.navigationsdkdemo;

import android.os.RemoteException;
import android.os.Bundle;
import android.util.Log;
import android.view.View;
import android.widget.EditText;
import android.widget.Toast;
import androidx.appcompat.app.AppCompatActivity;
import org.greenrobot.eventbus.EventBus;
import org.greenrobot.eventbus.Subscribe;
import org.greenrobot.eventbus.ThreadMode;

import java.util.List;

import cn.inbot.componentnavigation.domain.ActionStatus;
import cn.inbot.componentnavigation.domain.ForwardTargetPointStepInfo;
import cn.inbot.componentnavigation.domain.IndoorMapVo;
import cn.inbot.componentnavigation.domain.IndoorMapVoListResult;
import cn.inbot.componentnavigation.domain.NavigateInfo;
import cn.inbot.componentnavigation.domain.NavigateStatus;
import cn.inbot.componentnavigation.domain.NavigateStepInfo;
import cn.inbot.componentnavigation.domain.NavigateStepType;
import cn.inbot.componentnavigation.domain.RobotTargetPointVo;
import cn.inbot.navigationlib.PadBotNavigationClient;
import cn.inbot.navigationlib.event.OnNavigateInfoChangedEvent;
import cn.inbot.navigationlib.event.OnNavigationClientConnectedEvent;
import cn.inbot.navigationlib.event.OnNavigationInitedEvent;

public class MainActivity extends AppCompatActivity implements View.OnClickListener {

    private static final String TAG = "___NavigationSDK";

    private EditText pointNameET;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);
        EventBus.getDefault().register(this);

        pointNameET = findViewById(R.id.et_point_name);

        /*
         * Connect Navigation
         * 连接导航
         */
        PadBotNavigationClient.getInstance().connect(getApplicationContext());

    }

    @Override
    protected void onDestroy() {
        EventBus.getDefault().unregister(this);
        super.onDestroy();
    }

    /**
     * Receive navigation connection success event
     * 接收导航连接成功事件
     * @param event Navigation connection success event
     *              导航连接成功事件
     */
    @Subscribe(threadMode = ThreadMode.MAIN)
    public void onEvent(OnNavigationClientConnectedEvent event) {
        try {
            /*
             * Initialize navigation
             * 初始化导航
             */
            PadBotNavigationClient.getInstance().initNavigation();

        } catch (RemoteException e) {
            e.printStackTrace();
        }
    }

    /**
     * Received navigation initialization complete event
     * 接收到导航初始化完成事件
     * @param event Navigation initialization complete event
     *              导航初始化完成事件
     */
    @Subscribe(threadMode = ThreadMode.MAIN)
    public void onEvent(OnNavigationInitedEvent event) {

        try {

            /**
             * Get map information
             * 获取地图信息
             */
            IndoorMapVoListResult indoorMapVoListResult = PadBotNavigationClient.getInstance().getMapInfo();

        } catch (RemoteException e) {
            e.printStackTrace();
        }
    }


    @Override
    public void onClick(View v) {
        switch (v.getId()) {
            case R.id.btn_open_navigation_component:
                startNavigationApp();
                break;
            case R.id.btn_navigate_to_target_point:
                startNavigation();
                break;
            case R.id.btn_stop_navigating:
                stopNavigating();
                break;
        }
    }


    /**
     * Open navigation component
     * 打开导航组件
     */
    private void startNavigationApp() {
        PadBotNavigationClient.getInstance().startNavigationApp(this);
    }


    /**
     * Navigate to the target point
     * 导航去目标点
     */
    private void startNavigation() {

        try {

            String pointName = pointNameET.getEditableText().toString();
            if (null == pointName || "".equals(pointName.trim())) {
                showToast("Please input point name");
                return;
            }

            /*
             * Get map information
             * 获取地图信息
             */
            IndoorMapVoListResult indoorMapVoListResult = PadBotNavigationClient.getInstance().getMapInfo();
            if (null == indoorMapVoListResult || null == indoorMapVoListResult.getMapVoList()) {
                showToast("Failed to get map information");
                return;
            }

            /*
             * Get map List
             * 获取地图列表
             */
            List<IndoorMapVo> indoorMapVoList = indoorMapVoListResult.getMapVoList();
            if (indoorMapVoList.isEmpty()) {
                showToast("Please create a map first");
                return;
            }

            IndoorMapVo currentIndoorMap = null;
            //遍历地图列表，找到当前使用的地图
            for (IndoorMapVo indoorMapVo : indoorMapVoList) {
                if (indoorMapVo.isDefaultUse()) {
                    currentIndoorMap = indoorMapVo;
                    break;
                }
            }

            if (null == currentIndoorMap) {
                showToast("Failed to get default map information");
                return;
            }

            /*
             * Get list of target points in the map
             * 获取地图中的目标点列表
             */
            List<RobotTargetPointVo> targetPointVoList = currentIndoorMap.getTargetPointVoList();
            if (null == targetPointVoList || targetPointVoList.isEmpty()) {
                showToast("Please create a target point");
                return;
            }

            RobotTargetPointVo targetPointVo = null;
            for (RobotTargetPointVo pointVo : targetPointVoList) {
                if (pointName.equals(pointVo.getName())) {
                    targetPointVo = pointVo;
                    break;
                }
            }
            if (null == targetPointVo) {
                showToast("Please enter the correct target point name");
                return;
            }


            /*
             * Navigate to the target point
             * 导航去目标点
             */
            PadBotNavigationClient.getInstance().startNavigateByPointId(targetPointVo.getId());

        } catch (RemoteException e) {
            e.printStackTrace();
        }
    }

    /**
     * Stop navigating
     * 停止导航
     */
    private void stopNavigating() {
        try {
            PadBotNavigationClient.getInstance().stopNavigateOrAutoCharge();
        } catch (RemoteException e) {
            e.printStackTrace();
        }
    }

    /**
     * Received navigation information event
     * 接收到导航信息事件
     * @param event Navigation information event
     *              导航信息事件对象
     */
    @Subscribe(threadMode = ThreadMode.MAIN)
    public void onEvent(OnNavigateInfoChangedEvent event) {

        try {

            NavigateInfo navigateInfo = event.getNavigateInfo();
            NavigateStatus navigateStatus = navigateInfo.getNavigateStatus();
            RobotTargetPointVo targetPointVo = navigateInfo.getTargetPoint();
            NavigateStepType stepType = navigateInfo.getNavigateStepType();
            ActionStatus actionStatus = PadBotNavigationClient.getInstance().getActionStatus();

            Log.i(TAG,"navigateStatus:" + navigateStatus);

            /*
             * If the current action is to navigate to the target point
             * 如果当前的活动是导航去目标点
             */
            if (actionStatus == ActionStatus.NAVIGATING) {

                /*
                 * Start navigating to a new target point
                 * 开始导航去新的目标点
                 */
                if (NavigateStatus.NEW_TARGET == navigateStatus) {
                    if (null != targetPointVo) {
                        showToast("Navigate to:" + targetPointVo.getName());
                    }
                }
                /*
                 * Arrived the final target point
                 * 如果到达最终目标点
                 */
                else if (NavigateStatus.FINISHED == navigateStatus) {
                    if (null != targetPointVo) {
                        showToast("Arrived:" + targetPointVo.getName());
                    }
                }
            }

        } catch (RemoteException e) {
            e.printStackTrace();
        }
    }

    private void showToast(String content) {
        Toast.makeText(this, content, Toast.LENGTH_LONG).show();
    }

}
