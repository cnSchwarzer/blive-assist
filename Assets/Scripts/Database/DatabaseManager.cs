using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SQLite4Unity3d;
using UnityEngine;

public class DatabaseManager : MonoBehaviour {
    public static DatabaseManager Instance { get; private set; }

    private static Version currentRoomVersion = new Version(1, 4, 3); 

    public SQLiteConnection Room { get; private set; } 

    private void OnEnable() {
        Instance = this; 
    }

    public void InitRoom() {
        Room = new SQLiteConnection(Path.Combine(Application.persistentDataPath, $"database_{MainManager.Instance.room}.db"),
            SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex);
        try {
            var meta = Room.Table<DatabaseMeta>().First();
            Debug.Log($"Room {MainManager.Instance.room} Database Version " + meta.Version);
            if (Version.Parse(meta.Version) < currentRoomVersion) {
                UpdateRoomDatabaseMeta();
            }
        }
        catch (SQLiteException ex) {
            Debug.LogException(ex);
            UpdateRoomDatabaseMeta();
        }
    }

    public void ExitRoom() {
        Room?.Close();
        Room = null;
    }
    
    private void UpdateRoomDatabaseMeta() {
        Room.CreateTable<DatabaseMeta>();
        var table = Room.Table<DatabaseMeta>();
        if (!table.Any()) {
            Room.Insert(new DatabaseMeta());
        }

        var meta = table.First();
        meta.Version = currentRoomVersion.ToString();
        Room.Update(meta);
        
        Room.DropTable<Danmu>();
        Room.CreateTable<Superchat>();
        Room.CreateTable<Gift>(); 
    } 

    private void OnApplicationQuit() {
        Room?.Close(); 
    }

    public bool IsUserFiltered(int userId, User.Filter filter) {
        var userResult = Room.Table<User>().Where(u => u.Id == userId);
        if (!userResult.Any())
            return false;
        var user = userResult.First();
        return (user.Filtered & filter) != 0;
    }

    public void AddUserFilter(int userId, User.Filter filter) {
        var userResult = Room.Table<User>().Where(u => u.Id == userId);
        if (!userResult.Any())
            return;
        var user = userResult.First();
        user.Filtered |= filter;
        Room.Update(user);
    }

    public void RemoveUserFilter(int userId, User.Filter filter) {
        var userResult = Room.Table<User>().Where(u => u.Id == userId);
        if (!userResult.Any())
            return;
        var user = userResult.First();
        user.Filtered &= ~filter;
        Room.Update(user);
    }

    public void AddDanmu(Danmu danmu) {
        Room.Insert(danmu);
    }

    public void AddSuperchat(Superchat sc) {
        var v = Room.Table<Superchat>().FirstOrDefault(a => a.SuperchatId == sc.SuperchatId);
        if (v == null) {
            Room.Insert(sc);
        } else if (!string.IsNullOrWhiteSpace(sc.ContentJpn)) {
            v.ContentJpn = sc.ContentJpn;
            Room.Update(v);
        }
    }
    
    public void AddGift(Gift gift) {
        Room.Insert(gift);
    }
    
    public void RemoveGift(Gift gift) {
        Room.Delete(gift);
    }

    public void ClearSuperchat() {
        Room.DeleteAll<Superchat>(); 
    }
    
    public void ClearGift() {
        Room.DeleteAll<Gift>();
    }
}