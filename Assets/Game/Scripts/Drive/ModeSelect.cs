using CustomInspector;
using SFB;
using System.IO;
using Unity.Barracuda;
using Unity.Barracuda.ONNX;
using Unity.MLAgents.Policies;
using UnityEngine;


public class ModeSelect : MonoBehaviour
{
    [SerializeField, SelfFill] Canvas modeSelectCanvas;
    [SerializeField, ForceFill] Duplicator carDuplicator;
    [SerializeField, ForceFill] CarBrainAgent mainCarBrain;
    BehaviorParameters BehaviourParams => mainCarBrain.behaviorParameters;
    [SerializeField, Tooltip("Where the .onnx files are stored")] FolderPath defaultAISavePath = new();


    private void Start()
    {
        BehaviourParams.Model = null;
        if (!BehaviourParams.IsInHeuristicMode()) // if is in training
        {
            carDuplicator.gameObject.SetActive(true);
            modeSelectCanvas.gameObject.SetActive(false);
        }
    }
    public void DoHumanDrive()
    {
        BehaviourParams.Model = null;
        mainCarBrain.controlMode = CarBrainAgent.ControlMode.Human;
        modeSelectCanvas.gameObject.SetActive(false);
    }
    public void DoAlgDrive()
    {
        BehaviourParams.Model = null;
        mainCarBrain.controlMode = CarBrainAgent.ControlMode.Alg;
        modeSelectCanvas.gameObject.SetActive(false);
    }
    public void DoAiDrive()
    {
        // Opens the file panel to select an ONNX file
#if UNITY_EDITOR
        string defaultPath = defaultAISavePath.HasPath() ? defaultAISavePath.GetAbsolutePath() : Application.dataPath;
#else
        string defaultPath = Application.dataPath;
#endif
        string[] filePath = StandaloneFileBrowser.OpenFilePanel("Select an ONNX file", defaultPath, "onnx", multiselect: false);

        if (filePath != null && filePath.Length > 0 && !string.IsNullOrEmpty(filePath[0]))
        {
            string path = filePath[0];
            string modelName = Path.GetFileNameWithoutExtension(path);
            if (!File.Exists(path))
            {
                Debug.LogError("Model file not found: " + filePath);
                return;
            }
            // following code will get Model and convert it to NNModel using byte-stream

            // get model (of wrong type)

            ONNXModelConverter onnx = new(optimizeModel: true);
            Model model = onnx.Convert(path);

            using (var memoryStream = new MemoryStream())
            using (var writer = new BinaryWriter(memoryStream))
            {
                // generate byte array
                ModelWriter.Save(writer, model);
                byte[] bbb = memoryStream.ToArray();

                // parse byte to NNModel
                NNModel asset = ScriptableObject.CreateInstance<NNModel>();// just drag a  policy brain.
                asset.modelData = ScriptableObject.CreateInstance<NNModelData>();
                asset.modelData.Value = bbb;
                asset.name = modelName;
                asset.hideFlags = HideFlags.DontSave;

                // assign policy
                BehaviourParams.Model = asset;
            }

            // Debug.Log("Selected onnx file: " + filePath[0]);
            modeSelectCanvas.gameObject.SetActive(false);
        }
        else
        {
            Debug.Log("File selection canceled.");
        }
    }
    private void OnEnable()
    {
        Time.timeScale = 0;
    }
    private void OnDisable()
    {
        Time.timeScale = 1;
    }
}
