using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class GraphSaveUtility
{
    private DialogueGraphView m_dialogueGraphView;
    private DialogueContainer m_loadedDialogueContainer;

    private List<Edge> edges => m_dialogueGraphView.edges.ToList();
    private List<DialogueNode> dialogueNodes => m_dialogueGraphView.nodes.ToList().Cast<DialogueNode>().ToList();

    public static GraphSaveUtility GetInstance(DialogueGraphView graphView)
    {
        return new GraphSaveUtility
        {
            m_dialogueGraphView = graphView,
        };
    }

    public void SaveGraphView(string fileName)
    {
        //if there are no edges (no connections) then return
        if (!edges.Any()) return;

        var dialoguecontainer = ScriptableObject.CreateInstance<DialogueContainer>();
        var connectedPorts = edges.Where(x => x.input.node != null).ToArray();
        for (int i = 0; i < connectedPorts.Length; i++)
        {
            var outputNode = connectedPorts[i].output.node as DialogueNode;
            var inputNode = connectedPorts[i].input.node as DialogueNode;

            //if (outputNode.EntryPoint) continue;

            dialoguecontainer.nodeLinks.Add(new DialogueNodeLinkData
            {
                BaseNodeGUID = outputNode.GUID,
                PortName = connectedPorts[i].output.portName,
                Name = connectedPorts[i].output.name,
                TargetNodeGUID = inputNode.GUID,
            });
        }

        foreach (var dialogueNode in dialogueNodes.
            Where(node => !node.EntryPoint))
        {
            dialoguecontainer.dialogueLoadDatas.Add(new DialogueLoadData
            {
                NodeGUID = dialogueNode.GUID,
                DialogueText = dialogueNode.DialogueText,
                NodePositionAndSize = dialogueNode.GetPosition(),
            });
        }

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        AssetDatabase.CreateAsset(dialoguecontainer, $"Assets/Resources/{fileName}.asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public void LoadGraphView(string fileName)
    {
        m_loadedDialogueContainer = Resources.Load<DialogueContainer>(fileName);
        if (m_loadedDialogueContainer == null)
        {
            EditorUtility.DisplayDialog("File Not Found", "Target dialogue graph file does not exists ! ", "OK");
            return;
        }

        System.Action ClearGraph = () =>
        {
            // Set entry points guid back from the save. Discard existing guid
            dialogueNodes.Find(x => x.EntryPoint).GUID = m_loadedDialogueContainer.nodeLinks[0].BaseNodeGUID;

            foreach (var node in dialogueNodes)
            {
                if (node.EntryPoint) continue;

                edges.Where(x => x.input.node == node).ToList().
                    ForEach(edge => m_dialogueGraphView.RemoveElement(edge));

                m_dialogueGraphView.RemoveElement(node);
            }
        };

        System.Action CreateNodes = () =>
        {
            foreach (var nodeData in m_loadedDialogueContainer.dialogueLoadDatas)
            {
                var tempNode = m_dialogueGraphView.CreateDialogueNode(nodeData.DialogueText);
                tempNode.GUID = nodeData.NodeGUID;
                tempNode.SetPosition(nodeData.NodePositionAndSize);
                m_dialogueGraphView.AddElement(tempNode);

                var nodePorts = m_loadedDialogueContainer.nodeLinks.Where(x => x.BaseNodeGUID == nodeData.NodeGUID && x.BaseNodeGUID != x.TargetNodeGUID).ToList();
                //nodePorts.ForEach(x => m_dialogueGraphView.AddDialogueNodeChoicePort(tempNode, x.PortName));
                nodePorts.ForEach(x => m_dialogueGraphView.AddDialogueNodeChoicePort(tempNode, x.Name));
            }
        };

        System.Action ConnectNodes= () =>
        {
            System.Action<Port, Port> func_linkNodes = (inputPort, outputPort) =>
            {
                var tempEdge = new Edge
                {
                    output = outputPort,
                    input = inputPort,
                };
                tempEdge.input.Connect(tempEdge);
                tempEdge.output.Connect(tempEdge);

                m_dialogueGraphView.Add(tempEdge);
            };

            for (int i = 0; i < dialogueNodes.Count; i++)
            {
                var curNode = dialogueNodes[i];
                var connections = m_loadedDialogueContainer.nodeLinks.Where(
                    linkData => linkData.BaseNodeGUID == curNode.GUID
                ).ToList();
                for (int j = 0; j < connections.Count; j++)
                {
                    var curConnection = connections[j];
                    var targetNodeGuid = curConnection.TargetNodeGUID;
                    var targetNode = dialogueNodes.First(node => node.GUID == targetNodeGuid);

                    func_linkNodes(curNode.outputContainer[j].Q<Port>(), (Port)targetNode.inputContainer[0]);

                    targetNode.SetPosition(m_loadedDialogueContainer.dialogueLoadDatas.First(
                        node => node.NodeGUID == targetNode.GUID).NodePositionAndSize
                    );
                }
            }
        };

        ClearGraph();
        CreateNodes();
        ConnectNodes();
    }
}
