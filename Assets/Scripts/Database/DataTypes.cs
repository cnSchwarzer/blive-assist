using System;
using System.Collections.Generic;
using SQLite4Unity3d; 

public class UserOperation {
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public DateTime DateTime { get; set; }
    public string Message { get; set; }
}

public class User {
    [Flags]
    public enum Filter {
        None = 0,
        Danmu = 1,
        Superchat = 2,
        Gift = 4
    }

    [PrimaryKey] public int Id { get; set; } 
    public Filter Filtered { get; set; }
    public List<int> UserOperationIds { get; set; }
}

public class Danmu {
    [PrimaryKey, AutoIncrement] public int Id { get; set; }

    public DateTime Time { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; }
    public string Content { get; set; }
    public string MedalName { get; set; }
    public int MedalLevel { get; set; }
    public int GuardLevel { get; set; }
}

public class Superchat {
    [PrimaryKey, AutoIncrement] public int Id { get; set; }

    public int SuperchatId { get; set; }
    public DateTime Time { get; set; }
    public string BackgroundColor { get; set; }
    public string HeaderColor { get; set; }
    public string Content { get; set; }
    public string ContentJpn { get; set; }
    public int Price { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; }
    public string MedalName { get; set; }
    public int MedalLevel { get; set; }
    public int GuardLevel { get; set; }
    public string Face { get; set; }
    public string FaceFrame { get; set; }
    public bool Thanked { get; set; } = false;
}

public class Gift {
    [PrimaryKey, AutoIncrement] public int Id { get; set; }

    public string Action { get; set; }
    public DateTime Time { get; set; } 
    public string Name { get; set; }
    public float Currency { get; set; }
    public string Unit { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; }
    public string MedalName { get; set; }
    public int MedalLevel { get; set; }
    public int GuardLevel { get; set; } 
    public bool Thanked { get; set; } = false;
    
    [Ignore]
    public string ComboId { get; set; }
    [Ignore]
    public int Combo { get; set; }
    [Ignore]
    public bool IsGuardBuy { get; set; }
    [Ignore]
    public bool IsJoinRoom { get; set; }
    [Ignore]
    public bool IsComboSend { get; set; }
    [Ignore]
    public float Price {
        get {
            if (Unit == "gold")
                return Currency / 1000 * Combo;
            return 0;
        } 
    }
}

public class DatabaseMeta {
    [PrimaryKey, Unique] public int Id { get; set; } = 0;

    public string Version { get; set; }
} 