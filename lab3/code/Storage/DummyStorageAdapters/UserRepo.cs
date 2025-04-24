using Domain;
using Domain.OutPorts;
using Domain.Models;
using Storage.Models;
using Types;

namespace Storage.StorageAdapters;

public class DummyUserRepo : IUserRepo
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
        DBUser dbuser = new DBUser(u.NameID, u.Number.StringNumber, u.PasswordHash);
        Users[u.NameID] = dbuser;
        return true;
    }

    public bool TryUpdate(User u)
    {
        return true;
    }

    public bool TryDelete(string user_name)
    {
        Users.Remove(user_name);
        return true;
    }
}
