using Domain;
using Domain.OutPorts;
using Types;

namespace Storage.StorageAdapters;

public class UserRepo : IUserRepo
{
    private Dictionary<Guid, DBUser> Users = new();
    public User? Get(Guid id)
    {
        var dbuser = Users.GetValueOrDefault(id);
        if (dbuser == null)
            return null;
        User u = new User(dbuser.Id, dbuser.Name, dbuser.PasswordHash, new PhoneNumber(dbuser.Number));
        return u;
    }

    public User? Get(string username)
    {
        foreach (var dbuser in Users.Values)
        {
            if (dbuser.Name == username)
                return new User(dbuser.Id, dbuser.Name, dbuser.PasswordHash, new PhoneNumber(dbuser.Number));
        }
        return null;
    }

    public int Create(User u)
    {
        if (Users.ContainsKey(u.Id))
            return -1;
        foreach (var dbu in Users.Values)
        {
            if (dbu.Name == u.Name)
                return -1;
        }
        DBUser dbuser = new()
        {
            Id = u.Id,
            Name = u.Name,
            PasswordHash = u.PasswordHash,
            Number = u.Number.StringNumber
        };
        Users[u.Id] = dbuser;
        return 0;
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
