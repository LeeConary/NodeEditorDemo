using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogueContainer : ScriptableObject
{
    public List<DialogueLoadData> dialogueLoadDatas = new List<DialogueLoadData>();
    public List<DialogueNodeLinkData> nodeLinks = new List<DialogueNodeLinkData>();
}
