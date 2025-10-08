using UnityEngine;

[RequireComponent(typeof(Camera))]
public class TankHead_Camera : MonoBehaviour
{
    [Header("Follow Targets")]
    public Transform head;          // TankHead�i�e�FYaw & �ʒu��j
    public Transform barrelPivot;   // TankHead_BarrelPivot�i�q�FPitch��j

    [Header("View")]
    public Vector3 eyeOffset = new Vector3(0f, 0.2f, 0.1f); // ���̏����O�E��

    [Header("Clip & FOV")]
    public float nearClip = 0.03f;
    public float fieldOfView = 60f;

    Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.nearClipPlane = nearClip;
        cam.fieldOfView = fieldOfView;
    }

    void LateUpdate()
    {
        if (!head || !barrelPivot) return;

        // �ʒu�� TankHead ����ɖڐ��I�t�Z�b�g
        transform.position = head.TransformPoint(eyeOffset);

        // ��]�́u�C�g��forward�v�Ɍ�����i��Yaw�͐e�ɏ]���APitch�͖C�g�ɘA���j
        // ������x�N�g���̓��[���hUp�ɌŒ肵�ă��[���i�X���j��h��
        transform.rotation = Quaternion.LookRotation(barrelPivot.forward, Vector3.up);

        // ���������[�������e�������Ȃ灪�� barrelPivot.rotation �ɕς���
        // transform.rotation = barrelPivot.rotation; // ���[�����ǐ�
    }
}
