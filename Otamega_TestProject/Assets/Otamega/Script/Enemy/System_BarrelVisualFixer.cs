using UnityEngine;

/// <summary>
/// �C�g���b�V���̌����ڌ������Œ肷��i�s�b�`�͐ePivot���S���j
/// Mesh��-90�x��180�x�Y���Ă��鎞�ɕ␳�l�ŌŒ肵�āA���]��h���B
/// </summary>
public class System_BarrelVisualFixer : MonoBehaviour
{
    [Tooltip("���b�V���̌����ڒ����i�x�j�BYaw��Y���ARoll��Z���BPitch(X)�͐ePivot���񂵂܂��B")]
    public Vector3 meshFixEuler = new Vector3(0, 180, 0); // ��F���f�����������Ȃ� Y=180

    void LateUpdate()
    {
        // �e�iPivot�j������Ă��A���b�V���́g���̑��Ύp���h����Ɉێ�
        transform.localEulerAngles = meshFixEuler;
    }
}
