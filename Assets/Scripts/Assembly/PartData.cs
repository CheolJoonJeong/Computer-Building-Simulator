using UnityEngine;

public enum PartType { CPU, GPU, RAM, Mainboard, PSU, SSD, HDD, Cooler, Case, FrontPanel, BackPanel, PSUCover }

[CreateAssetMenu(menuName = "PC/PartData")]
public class PartData : ScriptableObject
{
    public string partName;
    public PartType partType;
}