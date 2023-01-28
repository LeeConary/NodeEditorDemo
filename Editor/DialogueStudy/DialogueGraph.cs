using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public enum DataOperation
{
    SAVE,
    LOAD,
}

public class DialogueGraph : EditorWindow 
{
    DialogueGraphView m_graphView;

    [MenuItem("GraphEditor/Dialogue Graph")]
    public static void OpenDialogueGraphWindow()
    {
        var window = GetWindow<DialogueGraph>();
        window.titleContent = new GUIContent("Dialogue Graph");
    }

    private void OnEnable()
    {
        ConstructGraph();
        GenerateToolbar();
    }
    private void OnDisable()
    {
        rootVisualElement.Remove(m_graphView);
    }

    void ConstructGraph()
    {
        m_graphView = new DialogueGraphView
        {
            name = "Dialogue Graph",
        };

        m_graphView.StretchToParentSize();
        rootVisualElement.Add(m_graphView);
    }

    string filename = "New Narritive";

    void GenerateToolbar()
    {
        var toolbar = new Toolbar();

        var createNodeBtn = new Button(() =>
        {
            var node = m_graphView.CreateDialogueNode("Test");
            m_graphView.AddNodeToGraphView(node);
        });
        createNodeBtn.text = "Create Dialogue Node";
        toolbar.Add(createNodeBtn);

        var fileNameTxt = new TextField("File name: ");
        fileNameTxt.SetValueWithoutNotify(filename);
        fileNameTxt.MarkDirtyRepaint();
        fileNameTxt.RegisterValueChangedCallback(evt => filename = evt.newValue);
        toolbar.Add(fileNameTxt);

        toolbar.Add(new Button(() => { RequestViewDataOperation(DataOperation.SAVE); }) { text = "Save Data" });
        toolbar.Add(new Button(() => { RequestViewDataOperation(DataOperation.LOAD); }) { text = "Load Data" });
        toolbar.Add(new Button(() => { m_graphView.RemoveAllNodes(); }) { text = "Clear Graph"});

        rootVisualElement.Add(toolbar);
    }

    void RequestViewDataOperation(DataOperation operation)
    {
        if (string.IsNullOrEmpty(filename))
        {
            EditorUtility.DisplayDialog("Invalid Filename !", "Please Input Filename And Try Again", "OK");
            return;
        }

        GraphSaveUtility saveUtility = GraphSaveUtility.GetInstance(m_graphView);

        switch (operation)
        {
            case DataOperation.SAVE:
                SaveData(saveUtility);
                break;
            case DataOperation.LOAD:
                LoadData(saveUtility);
                break;
            default:
                break;
        }
    }

    void SaveData(GraphSaveUtility saveUtility)
    {
        saveUtility.SaveGraphView(filename);
    }

    void LoadData(GraphSaveUtility saveUtility)
    {
        saveUtility.LoadGraphView(filename);
    }
}
