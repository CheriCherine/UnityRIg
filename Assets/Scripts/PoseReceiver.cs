using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class PoseReceiver : MonoBehaviour
{
    UdpClient client;
    public int port = 5065;  // 确保这个端口号与 Python 脚本中发送数据所用的端口号匹配

    // 线程安全队列存储接收到的数据
    private ConcurrentQueue<string> dataQueue = new ConcurrentQueue<string>();

    void Start()
    {
        client = new UdpClient(port);
        StartListening();
        Debug.Log("UDP Receiver started on port " + port);
    }

    void StartListening()
    {
        client.BeginReceive(ReceiveData, null);
    }

    void ReceiveData(IAsyncResult result)
    {
        IPEndPoint receiveIPGroup = new IPEndPoint(IPAddress.Any, port);
        byte[] received;
        try
        {
            received = client.EndReceive(result, ref receiveIPGroup);
            string receivedString = Encoding.UTF8.GetString(received);
            Debug.Log("Received: " + receivedString);  // 输出接收到的数据，以便确认数据正确性
            dataQueue.Enqueue(receivedString);  // 将数据加入队列
        }
        catch (Exception e)
        {
            Debug.LogError("Error receiving UDP data: " + e.Message);
        }
        finally
        {
            StartListening();  // 继续监听下一次数据
        }
    }

    void Update()
    {
        // 在主线程中处理所有接收到的数据
        while (dataQueue.TryDequeue(out string jsonData))
        {
            ParseData(jsonData);
        }
    }

    void ParseData(string jsonString)
    {
        try
        {
            LegPose pose = JsonUtility.FromJson<LegPose>(jsonString);
            Debug.Log($"Parsed Positions - Left Hip: {pose.left_leg.hip}, Right Hip: {pose.right_leg.hip}");
            Debug.Log($"Parsed Positions - Left Knee: {pose.left_leg.knee}, Right Knee: {pose.right_leg.knee}");
            Debug.Log($"Parsed Positions - Left Ankle: {pose.left_leg.ankle}, Right Ankle: {pose.right_leg.ankle}");
            Debug.Log($"Parsed Positions - Left Heel: {pose.left_leg.heel}, Right Heel: {pose.right_leg.heel}");
            Debug.Log($"Parsed Positions - Left Foot Index: {pose.left_leg.foot_index}, Right Foot Index: {pose.right_leg.foot_index}");
            ApplyPoseToModel(pose);
        }

        catch (Exception e)
        {
            Debug.LogError("Error parsing JSON data: " + e.Message);
        }
    }

    void ApplyPoseToModel(LegPose pose)
    {
        float modelReferenceDistance = 1.0f;  // 假设模型的参考距离是1米
        float receivedDistance = Vector3.Distance(pose.right_leg.knee, pose.right_leg.ankle);
        float scaleFactor = modelReferenceDistance / receivedDistance;

        // 使用缩放因子调整每个关节的位置
        UpdateJointPosition("LeftHip", pose.left_leg.hip, scaleFactor);
        UpdateJointPosition("RightHip", pose.right_leg.hip, scaleFactor);
        UpdateJointPosition("LeftKnee", pose.left_leg.knee, scaleFactor);
        UpdateJointPosition("RightKnee", pose.right_leg.knee, scaleFactor);
        UpdateJointPosition("LeftAnkle", pose.left_leg.ankle, scaleFactor);
        UpdateJointPosition("RightAnkle", pose.right_leg.ankle, scaleFactor);
        UpdateJointPosition("LeftHeel", pose.left_leg.heel, scaleFactor);
        UpdateJointPosition("RightHeel", pose.right_leg.heel, scaleFactor);
        UpdateJointPosition("LeftFootIndex", pose.left_leg.foot_index, scaleFactor);
        UpdateJointPosition("RightFootIndex", pose.right_leg.foot_index, scaleFactor);
        // Assuming that GameObjects for each joint have been correctly named in your Unity scene

        // Update positions for all joints
        Transform leftHipTransform = GameObject.Find("LeftHip").transform;
        Transform rightHipTransform = GameObject.Find("RightHip").transform;
        leftHipTransform.localPosition = new Vector3(pose.left_leg.hip.x, pose.left_leg.hip.y, pose.left_leg.hip.z);
        rightHipTransform.localPosition = new Vector3(pose.right_leg.hip.x, pose.right_leg.hip.y, pose.right_leg.hip.z);

        Transform leftKneeTransform = GameObject.Find("LeftKnee").transform;
        Transform rightKneeTransform = GameObject.Find("RightKnee").transform;
        leftKneeTransform.localPosition = new Vector3(pose.left_leg.knee.x, pose.left_leg.knee.y, pose.left_leg.knee.z);
        rightKneeTransform.localPosition = new Vector3(pose.right_leg.knee.x, pose.right_leg.knee.y, pose.right_leg.knee.z);

        Transform leftAnkleTransform = GameObject.Find("LeftAnkle").transform;
        Transform rightAnkleTransform = GameObject.Find("RightAnkle").transform;
        leftAnkleTransform.localPosition = new Vector3(pose.left_leg.ankle.x, pose.left_leg.ankle.y, pose.left_leg.ankle.z);
        rightAnkleTransform.localPosition = new Vector3(pose.right_leg.ankle.x, pose.right_leg.ankle.y, pose.right_leg.ankle.z);

        Transform leftHeelTransform = GameObject.Find("LeftHeel").transform;
        Transform rightHeelTransform = GameObject.Find("RightHeel").transform;
        leftHeelTransform.localPosition = new Vector3(pose.left_leg.heel.x, pose.left_leg.heel.y, pose.left_leg.heel.z);
        rightHeelTransform.localPosition = new Vector3(pose.right_leg.heel.x, pose.right_leg.heel.y, pose.right_leg.heel.z);

        Transform leftFootIndexTransform = GameObject.Find("LeftFootIndex").transform;
        Transform rightFootIndexTransform = GameObject.Find("RightFootIndex").transform;
        leftFootIndexTransform.localPosition = new Vector3(pose.left_leg.foot_index.x, pose.left_leg.foot_index.y, pose.left_leg.foot_index.z);
        rightFootIndexTransform.localPosition = new Vector3(pose.right_leg.foot_index.x, pose.right_leg.foot_index.y, pose.right_leg.foot_index.z);
    }
    void UpdateJointPosition(string jointName, Vector3 originalPosition, float scaleFactor)
    {
        Transform jointTransform = GameObject.Find(jointName).transform;
        jointTransform.localPosition = new Vector3(
            originalPosition.x * scaleFactor,
            originalPosition.y * scaleFactor,
            originalPosition.z * scaleFactor
        );
    }

    void OnApplicationQuit()
    {
        if (client != null)
        {
            Debug.Log("Closing UDP Client");
            client.Close();
        }
    }
}

[System.Serializable]
public class LegPose
{
    public JointData left_leg;
    public JointData right_leg;
}

[System.Serializable]
public class JointData
{
    public Vector3 hip;
    public Vector3 knee;
    public Vector3 ankle;
    public Vector3 heel;
    public Vector3 foot_index;
}
