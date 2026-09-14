using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class HardwareIntroDirector : MonoBehaviour
{
    public Camera renderCam;
    public Transform hmiTarget;
    public Transform plcTarget;
    public Transform vfdTarget;
    public Transform servoTarget;

    private void Start()
    {
        // For standalone execution
        StartCoroutine(IntroSequence());
    }

    public IEnumerator IntroSequence()
    {
        if (renderCam == null)
        {
            GameObject camObj = GameObject.Find("ControlCam_Close");
            if (camObj != null) renderCam = camObj.GetComponent<Camera>();
        }

        // HMI is on the door: 
        GameObject hmi = GameObject.Find("Delta_DOP_100WS_HMI");
        GameObject plc = GameObject.Find("Delta_AS320T_B_PLC");
        GameObject vfd = GameObject.Find("Delta_MS300_VFD");
        GameObject servo = GameObject.Find("Delta_ASD_A3_X_Rail");

        Vector3 startPos = new Vector3(-0.5f, 1.4f, 1.2f);
        
        // Sequence: 0-3s (HMI), 3-6s (PLC), 6-9s (VFD), 9-12s (Servo)
        yield return StartCoroutine(PanTo(hmi.transform, new Vector3(-0.7f, 1.2f, 0.5f), 3.0f));
        yield return StartCoroutine(PanTo(plc.transform, new Vector3(-0.2f, 1.2f, 0.1f), 3.0f));
        yield return StartCoroutine(PanTo(vfd.transform, new Vector3(-0.2f, 0.9f, 0.1f), 3.0f));
        yield return StartCoroutine(PanTo(servo.transform, new Vector3(0.0f, 1.2f, 0.1f), 3.0f));
    }

    private IEnumerator PanTo(Transform target, Vector3 camPos, float duration)
    {
        Vector3 initialPos = renderCam.transform.position;
        Quaternion initialRot = renderCam.transform.rotation;
        
        Quaternion targetRot = Quaternion.LookRotation((target.position - camPos).normalized, Vector3.up);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            renderCam.transform.position = Vector3.Lerp(initialPos, camPos, t);
            renderCam.transform.rotation = Quaternion.Slerp(initialRot, targetRot, t);
            yield return null;
        }
    }
}
