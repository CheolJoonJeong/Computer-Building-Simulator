using System;

[Serializable]
public class SelectedParts
{
    public string cpu;
    public string gpu;
    public string ram;
}

[Serializable]
public class QuoteData
{
    public string budget;
    public string purpose;
    public SelectedParts selected_parts;
}