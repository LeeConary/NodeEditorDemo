using Codice.CM.SEIDInfo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueGraphView : GraphView
{
	public static readonly Vector2 DEFAULT_NODE_SIZE = new Vector2(200, 150);
	public DialogueGraphView()
	{
		styleSheets.Add(Resources.Load<StyleSheet>("DialogueGraph"));
		SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

		var grid = new GridBackground();
		Insert(0, grid);
		grid.StretchToParentSize();

		this.AddManipulator(new ContentDragger());
		this.AddManipulator(new SelectionDragger());
		this.AddManipulator(new RectangleSelector());

		var entryNode = GenerateDefaultNode();
		AddElement(entryNode);
	}

	public void AddNodeToGraphView(Node node)
	{
		AddElement(node);
	}

	public void AddDialogueNodeChoicePort(DialogueNode tempNode, string name = null)
	{
        var generatePort = GeneratePort(tempNode, Direction.Output, typeof(string));

		//var oldLabel = generatePort.contentContainer.Q<Label>("type");

        var outputPortsCount = tempNode.outputContainer.Query("connector").ToList().Count;

		string choicePortName = string.IsNullOrEmpty(name) ?
            $"Choice {outputPortsCount}" : name;
        generatePort.portName = $"PIN-{outputPortsCount}";
        generatePort.name = choicePortName;

        var choiceTextField = new TextField
		{
			name = string.Empty,
			value = choicePortName,
		};
        //choiceTextField.RegisterValueChangedCallback(str => generatePort.portName = str.newValue);
        choiceTextField.RegisterValueChangedCallback(str => generatePort.name = str.newValue);
        //oldLabel.text = "PIN";
        generatePort.contentContainer.Add(choiceTextField);

		var deleteBtn = new Button(() => RemovePort(tempNode, generatePort))
		{
			text = "X",
		};
		generatePort.contentContainer.Add(deleteBtn);

        tempNode.outputContainer.Add(generatePort);
        tempNode.RefreshExpandedState();
        tempNode.RefreshPorts();
    }

    public DialogueNode CreateDialogueNode(string content)
	{
		DialogueNode node = new DialogueNode
		{
			title = content,
			DialogueText = content,
			GUID = Guid.NewGuid().ToString(),
		};

		var nodePort = GeneratePort(node, Direction.Input, typeof(string), Port.Capacity.Multi);
		nodePort.portName = "Input";
		node.inputContainer.Add(nodePort);

		var choiceBtn = new Button(() => { AddDialogueNodeChoicePort(node); });
		choiceBtn.text = "Add Choice";
		node.titleButtonContainer.Add(choiceBtn);

		node.RefreshExpandedState();
		node.RefreshPorts();
		node.SetPosition(new Rect(Vector2.zero, DEFAULT_NODE_SIZE));

		return node;
	}

	public void RemoveAllNodes()
	{
		List<DialogueNode> existingNodes = nodes.Cast<DialogueNode>().ToList();
		foreach (var tempNode in existingNodes)
		{
			if (tempNode.EntryPoint) 
			{
				var outputEdges = edges.Where(edge => edge.output.node == tempNode).ToList();
				if (outputEdges.Any())
				{
					var firstOutputEdge = outputEdges.First();
                     firstOutputEdge.output.DisconnectAll();
                    RemoveElement(firstOutputEdge);
                    continue;
                }
			}
			else
			{
                var nodeEdges = edges.Where(edge => edge.input.node == tempNode).ToList();
                nodeEdges.ForEach(edge =>
                {
                    edge.input.DisconnectAll();
                    RemoveElement(edge);
                });
                RemoveElement(tempNode);
            }
		}
	}

	public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
	{
		base.GetCompatiblePorts(startPort, nodeAdapter);

		List<Port> compatiblePorts = new List<Port>();
		ports.ForEach((port) =>
		{
			if (startPort != port && startPort.node != port.node)
			{
				compatiblePorts.Add(port);
			}
		});

		return compatiblePorts;
	}

	Port GeneratePort(DialogueNode node, Direction direction, Type varType, Port.Capacity capacity = Port.Capacity.Single)
	{
		return node.InstantiatePort(Orientation.Horizontal, direction, capacity, varType);
	}

	void RemovePort(DialogueNode node, Port port)
	{
		var targetEdge = edges.ToList().Where(tagNode => tagNode.output.portName == port.portName
			&& tagNode.output.node == node);
		if (targetEdge.Any())
		{
			var edge = targetEdge.First();
			edge.input.Disconnect(edge);
			RemoveElement(targetEdge.First());
		}

		node.outputContainer.Remove(port);
		node.RefreshExpandedState();
		node.RefreshPorts();
	}


    DialogueNode GenerateDefaultNode()
	{
		var node = new DialogueNode
		{
			title = "START",
			GUID = Guid.NewGuid().ToString(),
			DialogueText = "ENTRY POINT",
			EntryPoint = true,
		};

		Port outputPort = GeneratePort(node, Direction.Output, typeof(string));
		outputPort.portName = "Output";
        node.outputContainer.Add(outputPort);

		node.RefreshExpandedState();
		node.RefreshPorts();

		node.SetPosition(new Rect(100, 200, 200, 150));
		return node;
	}
}
