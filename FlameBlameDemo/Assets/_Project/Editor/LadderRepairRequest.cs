using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
namespace FlameBlame.EditorTools
{
    [InitializeOnLoad]
    public static class LadderRepairRequest
    {
        static TestRunnerApi runner;
        static LadderRepairRequest() {EditorApplication.update+=Tick;}
        static void Tick()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if(File.Exists("Temp/FlameBlame-RepairLadderLevel"))
            {
                File.Delete("Temp/FlameBlame-RepairLadderLevel");
                var ground=GameObject.Find("FlightExterior/GroundFloor");
                if(ground!=null)
                {
                    float top=ground.transform.position.y+ground.transform.localScale.y*.5f;
                    var floor=GameObject.Find("SecondFloorContents/Room/Floor").GetComponent<Renderer>();
                    float ceiling=floor.bounds.min.y-.02f;
                    if(top>ceiling) {float height=ground.transform.localScale.y-(top-ceiling); ground.transform.position=new Vector3(ground.transform.position.x,height*.5f,ground.transform.position.z); ground.transform.localScale=new Vector3(ground.transform.localScale.x,height,ground.transform.localScale.z);}
                    EditorSceneManager.MarkSceneDirty(ground.scene); EditorSceneManager.SaveScene(ground.scene);
                }
                File.WriteAllText("Captures/Ladder/repair-geometry.txt","GroundFloor top separated below room floor underside; room surface and materials unchanged.");
            }
            if(File.Exists("Temp/FlameBlame-TestLadderRepair"))
            {
                File.Delete("Temp/FlameBlame-TestLadderRepair"); runner=ScriptableObject.CreateInstance<TestRunnerApi>(); runner.RegisterCallbacks(new Results());
                runner.Execute(new ExecutionSettings(new Filter {testMode=TestMode.EditMode, assemblyNames=new[]{"FlameBlame.Tests.EditMode"}}));
            }
        }
        sealed class Results:ICallbacks
        {
            public void RunStarted(ITestAdaptor test) {} public void TestStarted(ITestAdaptor test) {} public void TestFinished(ITestResultAdaptor test) {if(test.TestStatus==TestStatus.Failed) File.AppendAllText("Captures/Ladder/repair-failures.txt",test.Test.FullName+"\n"+test.Message+"\n"+test.StackTrace+"\n");}
            public void RunFinished(ITestResultAdaptor result) {File.WriteAllText("Captures/Ladder/repair-tests.txt",result.TestStatus+" Passed="+result.PassCount+" Failed="+result.FailCount+"\n"+result.Message);}
        }
    }
}
