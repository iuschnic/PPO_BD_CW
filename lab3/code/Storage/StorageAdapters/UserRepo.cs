using Domain;
using Domain.OutPorts;
using Domain.Models;
using Storage.Models;
using Types;

namespace Storage.StorageAdapters;

public class UserRepo : IUserRepo
{
    private Dictionary<Guid, DBUser> Users = new();
    public User? TryGet(Guid id)
    {
        var dbuser = Users.GetValueOrDefault(id);
        if (dbuser == null)
            return null;
        User u = new User(dbuser.Id, dbuser.Name, dbuser.PasswordHash, new PhoneNumber(dbuser.Number));
        return u;
    }

    public User? TryGet(string username)
    {
        foreach (var dbuser in Users.Values)
        {
            if (dbuser.Name == username)
                return new User(dbuser.Id, dbuser.Name, dbuser.PasswordHash, new PhoneNumber(dbuser.Number));
        }
        return null;
    }

    public bool TryCreate(User u)
    {
        if (Users.ContainsKey(u.Id))
            return false;
        foreach (var dbu in Users.Values)
        {
            if (dbu.Name == u.Name)
                return false;
        }
        DBUser dbuser = new()
        {
            Id = u.Id,
            Name = u.Name,
            PasswordHash = u.PasswordHash,
            Number = u.Number.StringNumber
        };
        Users[u.Id] = dbuser;
        return true;
    }

    public void Update(User u)
    {
        return;
    }

    public void Delete(Guid id)
    {
        Users.Remove(id);
    }

    public void Save()
    {
        return;
    }
}
