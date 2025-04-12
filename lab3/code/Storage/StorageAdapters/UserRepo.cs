using Domain;
using Domain.OutPorts;
using Domain.Models;
using Storage.Models;
using Types;

namespace Storage.StorageAdapters;

public class UserRepo : IUserRepo
{
    //Nameid - User
    private Dictionary<string, DBUser> Users = new();

    public User? TryGet(string username)
    {
        foreach (var dbuser in Users.Values)
        {
            if (dbuser.NameID == username)
                return new User(dbuser.NameID, dbuser.PasswordHash, new PhoneNumber(dbuser.Number));
        }
        return null;
    }

    public bool TryCreate(User u)
    {
        if (Users.ContainsKey(u.NameID))
            return false;
        DBUser dbuser = new()
        {
            NameID = u.NameID,
            PasswordHash = u.PasswordHash,
            Number = u.Number.StringNumber
        };
        Users[u.NameID] = dbuser;
        return true;
    }

    public void Update(User u)
    {
        return;
    }

    public void Delete(string user_name)
    {
        Users.Remove(user_name);
    }

    public void Save()
    {
        return;
    }
}
