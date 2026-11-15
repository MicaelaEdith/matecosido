using System;
using UnityEngine;

public enum DialogueSpeaker
{
    Npc,
    Player
}

[Serializable]
public class DialogueLine
{
    public DialogueSpeaker speaker = DialogueSpeaker.Npc;

    [TextArea(2, 6)]
    public string text;

    [Range(0, 1)]
    public int playerTalkingVariant;
}

[CreateAssetMenu(fileName = "Dialogue", menuName = "Matecocido/Dialogue")]
public class DialogueData : ScriptableObject
{
    [SerializeField] private string npcName = "NPC";
    [SerializeField] private DialogueLine[] lines;

    [Tooltip("Si está activo, al terminar esta conversación el NPC deja de estar triste.")]
    [SerializeField] private bool npcStopsBeingSad = true;

    public string NpcName => npcName;
    public DialogueLine[] Lines => lines;
    public bool NpcStopsBeingSad => npcStopsBeingSad;
}
