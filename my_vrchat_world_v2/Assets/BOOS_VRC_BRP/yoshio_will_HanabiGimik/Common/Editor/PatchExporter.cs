using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using System.IO;
using System.Globalization;

public partial class PatchExporter : EditorWindow
{
    [SerializeField] private VisualTreeAsset VisualTreeAsset;
    [SerializeField] private StyleSheet StyleSheet;

    private ObjectField _targetFolderField;
    private TextField _selectDateField;

    const string DateTimeFormat = "yyyy/MM/dd HH:mm";


    [MenuItem("Tools/Chikuwa-ya/Patch Exporter")]
    private static void PatchExporterWindow()
    {
        var window = GetWindow<PatchExporter>("UIElements");
        window.titleContent = new GUIContent("Patch Exporter");
        window.Show();
    }

    private void CreateGUI()
    {
        VisualTreeAsset.CloneTree(rootVisualElement);
        rootVisualElement.styleSheets.Add(StyleSheet);

        _targetFolderField = rootVisualElement.Q<ObjectField>("TargetFolder");
        _selectDateField = rootVisualElement.Q<TextField>("SinceDateTime");

        var button = rootVisualElement.Q<Button>("SelectFilesButton");
        button.clicked += OnSelectFiles;

        _selectDateField.value = DateTime.Now.ToString(DateTimeFormat);
    }

    public void OnSelectFiles()
    {
        UnityEngine.Object obj = _targetFolderField.value;
        if (!obj)
        {
            Debug.LogWarning("Object is not specified");
            return;
        }
        if (obj.GetType() != typeof(DefaultAsset))
        {
            Debug.LogWarning("Specified Object is not folder (type)");
            return;
        }

        DefaultAsset folder = (DefaultAsset)obj;
        string folderPath = AssetDatabase.GetAssetPath(folder);
        if (!File.GetAttributes(folderPath).HasFlag(FileAttributes.Directory))
        {
            Debug.LogWarning("Specified Object is not folder (Filesystem Attribute)");
            return;
        }

        DateTime sinceDate;
        if (!DateTime.TryParseExact(_selectDateField.value, DateTimeFormat, CultureInfo.CurrentCulture, DateTimeStyles.None, out sinceDate))
        {
            Debug.LogWarning("Invalid DateTime format");
            return;
        }

        UnityEngine.Object[] objects = new UnityEngine.Object[0];
        SelectFilesSub(folderPath, sinceDate, ref objects);
        Selection.objects = objects;
    }

    private void SelectFilesSub(string folder, DateTime sinceDate, ref UnityEngine.Object[] objects)
    {
        string[] allFiles = Directory.GetFiles(folder);
        foreach(var file in allFiles)
        {
            DateTime lastWriteTime = File.GetLastWriteTime(file);
            if (lastWriteTime < sinceDate) continue;

            string ext = Path.GetExtension(file);
            string withoutExt = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file));
            string csfile = withoutExt + ".cs";

            switch(ext.ToLower())
            {
                case ".meta": continue;     // .meta ファイルは除外する

                case ".asset":
                    if (File.Exists(csfile))
                    {
                        // 対応するcsファイルがある場合、
                        // csが新しければcsとassetを選択に加える
                        // csが古かったらcsもassetも選択に加え"ない"
                        lastWriteTime = File.GetLastWriteTime(csfile);
                        if (lastWriteTime < sinceDate) continue;
                        AddFile(ref objects, csfile);
                    }
                    // 対応するcsが無い場合はそのまま選択に加える
                    break;
            }

            AddFile(ref objects, file);
        }

        allFiles = Directory.GetDirectories(folder);
        foreach (var file in allFiles)
        {
            // 再帰
            SelectFilesSub(file, sinceDate, ref objects);
        }
    }

    private void AddFile(ref UnityEngine.Object[] objects, string path)
    {
        UnityEngine.Object newObj = AssetDatabase.LoadMainAssetAtPath(path);
        Array.Resize(ref objects, objects.Length + 1);
        objects[objects.Length - 1] = newObj;
    }


}
