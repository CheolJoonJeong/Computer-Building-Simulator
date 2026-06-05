// 케이블 종류
public enum CableType
{
    ATX24Pin,    // PSU -> Mainboard (24-pin Main Power)
    CPU8Pin,     // PSU -> Mainboard (8-pin +12V EPS)
    PCIe8Pin,    // PSU -> GPU
    FanHeader,   // Mainboard CPU_FAN -> Cooler

    // 케이스 전면 -> 메인보드 System Panel (PANEL)
    PWRSW,       // 전원 버튼 (2-pin)
    RESET,       // 리셋 버튼 (2-pin)
    PLED,        // 전원 LED (2-pin)
    HDD_LED,     // 저장장치 LED (2-pin)

    FrontUSB3    // 케이스 전면 -> 메인보드 USB 3.2 Gen1 (U32G1)
}
