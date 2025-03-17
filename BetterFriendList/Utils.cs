using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterFriendList;

public static class Extensions
{
    public static string toBaliseString(this Icons icon)
    {
        if (icon == Icons.None)
            return "";
        return $"<icon({(int)icon})>";
    }
}

public enum Icons
{
    None = 0,
    Limsa = 51,
    Gridania = 52,
    Uldah = 53,
    GLA = 128,
    PGL = 129,
    MRD = 130,
    LNC = 131,
    ARC = 132,
    CNJ = 133,
    THM = 134,
    CRP = 135,
    BSM = 136,
    ARM = 137,
    GSM = 138,
    LTW = 139,
    WVR = 140,
    ALC = 141,
    CUL = 142,
    MIN = 143,
    BTN = 144,
    FSH = 145,
    PLD = 146,
    MNK = 147,
    WAR = 148,
    DRG = 149,
    BRD = 150,
    WHM = 151,
    BLM = 152,
    ACN = 153,
    SMN = 154,
    SCH = 155,
    ROG = 156,
    NIN = 157,
    MCH = 158,
    DRK = 159,
    AST = 160,
    SAM = 161,
    RDM = 162,
    BLU = 163,
    GNB = 164,
    DNC = 165,
    RPR = 166,
    SGE = 167,
    VRP = 170,
    PCT = 171
}
