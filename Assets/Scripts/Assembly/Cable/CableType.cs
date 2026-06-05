// 케이블 종류
public enum CableType
{
    ATX24Pin,    // PSU -> Mainboard
    CPU8Pin,     // PSU -> Mainboard
    PCIe8Pin,    // PSU -> GPU
    FanHeader,   // Mainboard -> Cooler
    FrontPanel   // Case Front Panel -> Mainboard JFP1 (전원 버튼 등)
}
