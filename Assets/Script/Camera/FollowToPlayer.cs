using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowToPlayer : MonoBehaviour
{
    [Header("目标角色")]
    public Transform target; // 需要摄像机跟随的玩家目标

    [Header("2.5D固定视角参数")]
    public Vector3 offset = new Vector3(0, 5.11f, -6f); // 摄像机与玩家之间保持的固定位置偏移
    public float followSpeed = 5f; // 摄像机移动到目标位置时的平滑速度
    public Vector3 fixedEulerAngles = new Vector3(31.2f, 0f, 0f); // 2.5D视角下摄像机固定死的旋转角度

    private void Start()
    {
        // 游戏开始后隐藏鼠标，避免鼠标指针挡住画面。
        Cursor.visible = false;

        // 锁定鼠标到游戏窗口中，防止玩家操作时鼠标跑到窗口外。
        // 在编辑器 Play 模式里可以按 Esc 让鼠标重新显示。
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void LateUpdate()
    {
        if (target == null) return; // 没有目标时不执行跟随，避免空引用报错

        Vector3 targetPosition = target.position + offset; // 根据玩家位置和固定偏移计算摄像机应该移动到的位置

        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime); // 平滑移动摄像机到目标位置

        transform.rotation = Quaternion.Euler(fixedEulerAngles); // 强制使用固定2.5D视角，不再根据玩家方向旋转
    }
}
