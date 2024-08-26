#if UNITY_EDITOR
//#define LOG
#define Cell
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Text;

namespace ET
{
    public class FrameCode : EditorWindow
    {
        #region GUI  [MenuItem("CodeGenerationTools/FrameCode")]  创建一个菜单项，在Unity菜单栏中显示

        [MenuItem("CodeGenerationTools/FrameCode")] // 创建一个菜单项，在Unity菜单栏中显示
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(FrameCode)); // 创建并显示自定义Editor窗口
        }

        #region GUI成员变量

        private bool _dataCompState = false;
        private bool _overwriteState = false;
        private bool _cellState = false;

        #endregion

        void OnGUI()
        {
            GUILayout.Label("拖入需要生成代码的prefab", EditorStyles.boldLabel);

            Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "将prefab拖入此处");

            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition))
                        return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
                        {
                            _prefabPath = AssetDatabase.GetAssetPath(draggedObject);
                            _fileName = System.IO.Path.GetFileName(_prefabPath);
                        }
                    }

                    Event.current.Use();
                    break;
            }

            GUILayout.Space(20);
            GUILayout.Label("File Name: " + _fileName, EditorStyles.label);
            
            GUILayout.BeginHorizontal();
            _dataCompState = GUILayout.Toggle(_dataCompState, "生成DataComponent代码");
            _overwriteState = GUILayout.Toggle(_overwriteState, "重新生成并覆盖所有代码（谨慎勾选）");
            GUILayout.EndHorizontal();
            _cellState = GUILayout.Toggle(_cellState, "生成Cell代码");
            GUILayout.Label("1.添加的DataComponent记得在AfterCreateZoneScene_AddDataComponent中AddComponent");
            GUILayout.Label(
                "2.同一个Form下的Cell不能同名，不然只能生成一个Cell代码,另外一个Cell会被忽略;\n比如TestMainForm下面有一个节点Scr，\n它的cell的name为Cell，还有一个节点Scr，它的也为Cell，那么就只会生成一个TestMainCell；\n示例：将第一个Cell命名为OneCell，第二个命名为TwoCell",
                GUILayout.ExpandWidth(true));
            
            
            
            if (GUILayout.Button("生成代码"))
            {
                GenerateCode();
            }
        }

        #endregion

        #region 模板代码

        private const string FormSystemCode =
            "using UnityEngine.UI;using UnityEngine;using System.Collections.Generic;namespace ET{public class UILuckyTurntableMainFormSystem : AUI<LuckyTurntableMainForm> { protected override void OnInit(LuckyTurntableMainForm a) {a.OnInit(); }protected override void OnLoad(LuckyTurntableMainForm a){a.OnInit();} protected override void OnShow(LuckyTurntableMainForm a){a.OnShow();}protected override void OnHide(LuckyTurntableMainForm a){a.OnHide();} }public static class LuckyTurntableMainFormSystem {public static void OnInit(this LuckyTurntableMainForm self){ ***CodeCodeCode*** }public static void OnShow(this LuckyTurntableMainForm self){}public static void OnHide(this LuckyTurntableMainForm self){}}}";

        private const string FormCode =
            "using UnityEngine.UI;using UnityEngine;using System.Collections.Generic;namespace ET{public class LuckyTurntableMainForm : UIForm{***CodeCodeCode***}}";

        private const string WindowSystemCode =
            "using UnityEngine;namespace ET{public class UILuckyTurntableWindowSystem : AUI<LuckyTurntableWindow>{protected override void OnInit(LuckyTurntableWindow a){a.OnInit();}protected override void OnShow(LuckyTurntableWindow a){a.OnShow();}protected override void OnHide(LuckyTurntableWindow a){a.OnHide();}protected override void OnDestroy(LuckyTurntableWindow a){a.OnDestroy();}}public static class LuckyTurntableWindowHelper{public static void OpenWindow(){}}public static class LuckyTurntableWindowSystem{public static void OnInit(this LuckyTurntableWindow self){***CodeCodeCode***}public static void OnShow(this LuckyTurntableWindow self){}static void OpenWhere(this LuckyTurntableWindow self){}public static void OnHide(this LuckyTurntableWindow self){}public static void OnDestroy(this LuckyTurntableWindow self){}}}";

        private const string WindowCode = 
            "namespace ET{[UILayer(UILayerType.Window)]public class LuckyTurntableWindow : UIWindow{}}";

        private const string DataCompCode = "namespace ET { public class LuckyTurntableDataComponent : Entity, IDestroy {}}";

        private const string DataCompSystemCode =
            "namespace ET{public class LuckyTurntableDataComponentDestroySystem : DestroySystem<LuckyTurntableDataComponent> { public override void Destroy(LuckyTurntableDataComponent self){self.Destroy();}}public static class LuckyTurntableDataComponentSystem {public static void Destroy(this LuckyTurntableDataComponent self){self.Clear();}static void Clear(this LuckyTurntableDataComponent self) {}}}";

        private const string CellCode =
            "using UnityEngine.UI;using UnityEngine;namespace ET{public class LuckyTurntableMainCell : UICell{***CodeCodeCode***}}";

        private const string CellSystemCode =
            "using UnityEngine;using UnityEngine.UI;using System.Collections.Generic;namespace ET{public class UILuckyTurntableMainCellSystem : ACell<LuckyTurntableMainCell>{protected override void OnInit(LuckyTurntableMainCell a){a.OnInit();}protected override void OnLoad(LuckyTurntableMainCell a){a.OnInit();}protected override void OnFlush(LuckyTurntableMainCell a){a.OnFlush();}}public static class LuckyTurntableMainCellSystem{public static void OnInit(this LuckyTurntableMainCell self){***CodeCodeCode***}public static void OnFlush(this LuckyTurntableMainCell self){}}}";


        #endregion

        #region 编辑器成员变量

        private string _prefabPath = "";
        private string _fileName = "";
        private const string SystemPath = @"Assets\Scripts\HotfixView\Work\UI";
        private const string Path = @"Assets\Scripts\ModelView\Work\UI";
        private const string DataCompPath = @"Assets\Scripts\Model\Work\Data";
        private const string DataCompSystemPath = @"Assets\Scripts\Hotfix\Work\Data";

        private const string SearchFormString = "LuckyTurntableMainForm";
        private const string SearchWindowString = "LuckyTurntableWindow";
        private const string SearchDataCompString = "LuckyTurntable";
        private const string SearchCellString = "LuckyTurntableMainCell";

        private const string CodeFlag = "***CodeCodeCode***"; //在模板代码中需要替换的部分
#if !LOG
        private string _replaceString = "";
#endif
        public static string NameWithoutSuffix;

        #endregion
        
        private void GenerateCode()
        {
            ParsePrefab.Clear();

            if (string.IsNullOrEmpty(_fileName))
            {
                Debug.LogError("file is null or empty! check the file path!");
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(_prefabPath);
            if (prefab == null)
            {
                Debug.LogError("cannot find the prefab , check the file path!");
                return;
            }

            var rc = prefab.GetComponent<ReferenceCollector>();
            if (rc == null)
            {
                Debug.LogError("the prefab without ReferenceCollector Component!");
                return;
            }

            int index = _fileName.IndexOf("Window");
            NameWithoutSuffix = _fileName.Substring(0, index); //去除Window后缀,kof
            
            ParsePrefab.ReferenceCollectorParser_Static(prefab.name, rc);
#if LOG
            foreach (var code in ParsePrefab.Code)
            {
                Debug.Log(code.Key+"\n" + code.Value);
            }
#else
                            
            string createSystemPath = SystemPath + "\\" + NameWithoutSuffix; //System需要创建的文件夹的路径

            string createPath = Path + "\\" + NameWithoutSuffix; //需要创建的文件夹路径


            if (!Directory.Exists(createPath))
            {
                Directory.CreateDirectory(createPath);
            }

            if (!Directory.Exists(createSystemPath))
            {
                Directory.CreateDirectory(createSystemPath);
            }

            for (int i = 0; i < ParsePrefab.FormName.Count; i++)
            {
                _replaceString = NameWithoutSuffix + ParsePrefab.FormName[i]; //LuckyTurntableMainForm,kofMainFom

                try
                {
                    //Form :
                    {
                        string formFrameCode = FormCode.Replace(SearchFormString, _replaceString);
                        string formFinalCode = formFrameCode.Replace(CodeFlag, _replaceString.GetCodeByName());
                        string result = FileOperation.CreateFile(createPath, _replaceString + ".cs", _overwriteState)
                                            ?.WriteCodeToFile(formFinalCode) ??
                                        "<color=yellow>File is already existed!</color>\n" + createPath + "\\" +
                                        _replaceString + ".cs";
                        Debug.Log(result);
                    }

                    //FormSystem :
                    {
                        string formSystemFrameCode = FormSystemCode.Replace(SearchFormString, _replaceString);
                        string formSystemFinalCode =
                            formSystemFrameCode.Replace(CodeFlag, _replaceString.GetCodeByName("System"));
                        string result = FileOperation.CreateFile(createSystemPath, _replaceString + "System.cs", _overwriteState)
                                            ?.WriteCodeToFile(formSystemFinalCode) ??
                                        "<color=yellow>File is already existed!</color>\n" + createSystemPath + "\\" +
                                        _replaceString + "System.cs";
                        Debug.Log(result);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    return;
                }

                _replaceString = "";
            }

            _replaceString = "";

            try
            {
                _replaceString = _fileName.Substring(0, _fileName.IndexOf(".prefab"));

                //Window :
                {
                    int layer = ParsePrefab.GetLayer(prefab);
                    string windowFrameCode = WindowCode.ReplaceIncludeLayer(SearchWindowString, _replaceString, layer);
                    string windowFinalCode = windowFrameCode.Replace(CodeFlag, _replaceString.GetCodeByName());
                    string result = FileOperation.CreateFile(createPath, _replaceString + ".cs", _overwriteState)
                                        ?.WriteCodeToFile(windowFinalCode) ??
                                    "<color=yellow>File is already existed!</color>\n" + createPath + "\\" +
                                    _replaceString + ".cs";
                    Debug.Log(result);
                }

                //WindowSystem:
                {
                    string windowSystemFrameCode = WindowSystemCode.Replace(SearchWindowString, _replaceString);
                    string windowSystemFinalCode =
                        windowSystemFrameCode.Replace(CodeFlag, _replaceString.GetCodeByName("System"));
                    string result = FileOperation.CreateFile(createSystemPath, _replaceString + "System.cs", _overwriteState)
                                        ?.WriteCodeToFile(windowSystemFinalCode) ??
                                    "<color=yellow>File is already existed!</color>\n" + createSystemPath + "\\" +
                                    _replaceString + "System.cs";
                    Debug.Log(result);
                }

                
                if (_dataCompState)
                {
                    //dataComponent:
                    {
                        string dataCompFrameCode = DataCompCode.Replace(SearchDataCompString, NameWithoutSuffix);
                        string result = FileOperation.CreateFile(DataCompPath, NameWithoutSuffix + "DataComponent.cs", _overwriteState)?.WriteCodeToFile(dataCompFrameCode) ??
                                        "<color=yellow>File is already existed!</color>\n" + DataCompPath + "\\" + NameWithoutSuffix + "DataComponent.cs";
                        Debug.Log(result);
                    }
                    //dataComponentSystem:
                    {
                        string dataCompSystemFrameCode = DataCompSystemCode.Replace(SearchDataCompString, NameWithoutSuffix);
                        string result = FileOperation.CreateFile(DataCompSystemPath, NameWithoutSuffix + "DataComponentSystem.cs", _overwriteState)?.WriteCodeToFile(dataCompSystemFrameCode) ??
                                        "<color=yellow>File is already existed!</color>\n" + DataCompPath + "\\" + NameWithoutSuffix + "DataComponentSystem.cs";
                        Debug.Log(result);
                    }
                }

                if(_cellState)
                {
                    for (int i = 0; i < ParsePrefab.CellName.Count; i++)
                    {
                        _replaceString = NameWithoutSuffix + ParsePrefab.CellName[i];

                        //Cell :
                        {
                            string formFrameCode = CellCode.Replace(SearchCellString, _replaceString);
                            string formFinalCode = formFrameCode.Replace(CodeFlag, _replaceString.GetCodeByName());
#if Cell
                            string result = FileOperation
                                                .CreateFile(createPath, _replaceString + ".cs", _overwriteState)
                                                ?.WriteCodeToFile(formFinalCode) ??
                                            "<color=yellow>file is already existed!</color>\n" + createPath + "\\" +
                                            _replaceString + ".cs";
                            Debug.Log(result);
#else
                        Debug.Log(FileOperation.CreateFile(createPath, _replaceString + ".cs", true) + " - " + formFinalCode);
#endif

                        }

                        //CellSystem :
                        {
                            string cellSystemFrameCode = CellSystemCode.Replace(SearchCellString, _replaceString);
                            string cellSystemFinalCode =
                                cellSystemFrameCode.Replace(CodeFlag, _replaceString.GetCodeByName("System"));
#if Cell
                            string result = FileOperation
                                                .CreateFile(createSystemPath, _replaceString + "System.cs",
                                                    _overwriteState)
                                                ?.WriteCodeToFile(cellSystemFinalCode) ??
                                            "<color=yellow>file is already existed!</color>\n" + createSystemPath +
                                            "\\" +
                                            _replaceString + "System.cs";
                            Debug.Log(result);
#else
                        Debug.Log(FileOperation.CreateFile(createSystemPath, _replaceString + "System.cs", true) + " - "+ cellSystemFinalCode);
#endif

                        }
                    }
                }

            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return;
            }

            Debug.Log("Generate Code successfully!");
            ParsePrefab.Clear();
#endif 
        }
    }

    public static class FileOperation
    {
        /// <summary>
        /// 返回创建好的文件的路径；如果文件已经存在且isOverwrite为true，则返回该文件路径；创建失败返回null,仅在unity编辑器下使用，不建议在FrameCode.cs外使用
        /// </summary>
        /// <param name="path"></param>
        /// <param name="file"></param>
        /// <param name="isOverwrite"></param>
        /// <returns></returns>
        public static string CreateFile(string path, string file,bool isOverwrite)
        {
            string cs = System.IO.Path.Combine(path, file);
            
            if (File.Exists(cs) && !isOverwrite)
            {
                return null;
            }

            if(!isOverwrite)
            {
                File.Create(cs).Dispose();
            }

            return cs;
        }

        /// <summary>
        /// 将代码输入到csFilePath中，仅在unity编辑器下使用，不建议在FrameCode.cs外使用
        /// </summary>
        /// <param name="csFilePath"></param>
        /// <param name="code"></param>
        public static string WriteCodeToFile(this string csFilePath, string code)
        {
            using (StreamWriter sw = new StreamWriter(csFilePath))
            {
                sw.Write(code);
            }

            return "<color=cyan>Write into the file: </color>\n" + csFilePath;
        }
    }

    public static class StringExtend
    {
        /// <summary>
        /// 仅在unity编辑器下使用，不建议在FrameCode.cs外使用
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string RemovePrefix(this string name)
        {
            if (!name.Contains("Window"))
            {
                return name.Substring(FrameCode.NameWithoutSuffix.Length); //去除前缀
            }

            return name;
        }

        /// <summary>
        /// 仅在unity编辑器下使用，不建议在FrameCode.cs外使用
        /// </summary>
        /// <param name="name"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public static string GetCodeByName(this string name, string suffix = "")
        {
            string nameWithoutPrefix = name.RemovePrefix();
            return ParsePrefab.Code[nameWithoutPrefix + suffix];
        }

        public static string ReplaceIncludeLayer(this string originalStr, string oldValue, string newValue,int layer)
        {
            string str = originalStr.Replace("UILayerType.Window", layer.GetUILayerType());

            return str.Replace(oldValue, newValue);
        }
    }


    public static class IntExtend
    {
        /// <summary>
        /// 仅在unity编辑器下使用，不建议在FrameCode.cs外使用
        /// </summary>
        public static string GetUILayerType(this int layer)
        {
            if (layer >= 4000)
            {
                return "UILayerType.Tip";
            }

            if (layer >= 3000)
            {
                return "UILayerType.Pop";
            }

            return "UILayerType.Window";
        }
    }
}
#endif