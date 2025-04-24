using Domain.Models;
using Domain.OutPorts;
using Storage.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Types;

namespace Storage.PostgresStorageAdapters;

public class PostgresUserRepo : IUserRepo
{
    //Nameid - User
    //private Dictionary<string, DBUser> Users = new();
    PostgresDBContext _dbContext;

    public PostgresUserRepo(PostgresDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public User? TryGet(string username)
    {
        var dbuser = _dbContext.Users.Find(username);
        if (dbuser == null)
            return null;
        return new User(dbuser.NameID, dbuser.PasswordHash, new PhoneNumber(dbuser.Number));
    }

    public bool TryCreate(User u)
    {
        DBUser? dbu = _dbContext.Users.Find(u.NameID);
        if (dbu != null)
            return false;
        DBUser dbuser = new DBUser(u.NameID, u.Number.StringNumber, u.PasswordHash);
        _dbContext.Users.Add(dbuser);
        _dbContext.SaveChanges();
        return true;
    }

    public bool TryUpdate(User u)
    {
        var dbu = _dbContext.Users.Find(u.NameID);
        if (dbu == null)
            return false;
        dbu.Number = u.Number.StringNumber;
        dbu.PasswordHash = u.PasswordHash;
        _dbContext.SaveChanges();
        return true;
    }

    public bool TryDelete(string user_name)
    {
        //Users.Remove(user_name);
        var dbu = _dbContext.Users.Find(user_name);
        if (dbu == null)
            return false;
        _dbContext.Users.Remove(dbu);
        _dbContext.SaveChanges();
        return true;
    }
}
