using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace SentinelData
{
    public class SentinelData
    {
        private const string MongoDBConnectionString =
            "mongodb+srv://Sentinel:jND7BvTajneVHou1@myperformance-54wls.azure.mongodb.net/test?retryWrites=true";
        private IClientSessionHandle session;
        public SentinelData()
        {
            //Create client connection to our MongoDB database
            var client = new MongoClient(MongoDBConnectionString);
            //Create a session object that is used when leveraging transactions
            session = client.StartSession();
        }

        //public List<User> GetUsers()
        //{
        //    //Create the collection object that represents the "users" collection
        //    var users = session.Client.GetDatabase("MyPerformance").GetCollection<User>("users");
        //    return users;
        //}

        public async Task<List<User>> InitUsers(List<User> existingUsers)
        {
            var users = session.Client.GetDatabase("MyPerformance").GetCollection<User>("users");
            //Clean up the collection if there is data in there
           // users.Database.DropCollection("users");

            PrincipalContext ctx = new PrincipalContext(ContextType.Machine, Environment.MachineName);
            UserPrincipal user = new UserPrincipal(ctx);
            user.Name = "*";
            PrincipalSearcher ps = new PrincipalSearcher();
            ps.QueryFilter = user;
            PrincipalSearchResult<Principal> result = ps.FindAll();
        
            var machineId = Machine.GetMachineId();
            try
            {
                Console.WriteLine("User Accounts\n");

                foreach (Principal p in result)
                {
                    using (UserPrincipal mo = (UserPrincipal) p)
                    {
                        Console.WriteLine("Account : " + mo.Name);
                        //Begin transaction
                        Console.WriteLine("Local, checking if already exists");
                        // check if the user already exists
                        if (existingUsers.Any(_ => _.Name == mo.Name.ToString()))
                        {
                            continue;
                        }

                        Console.WriteLine("Adding : " + mo.Name);
                        session.StartTransaction();
                        try
                        {
                            var newUser = new User
                            {
                                Name = mo.Name,
                                MachineId = machineId,
                                SamAccountName = mo.SamAccountName,
                                DisplayName = mo.DisplayName,
                                SID = mo.Sid.ToString(),
                                UserPrincipalName = mo.UserPrincipalName,
                                Description = mo.Description
                            };
                            await users.InsertOneAsync(newUser);
                            session.CommitTransaction();

                            existingUsers.Add(newUser);
                        }
                        catch (Exception e)
                        {
                            session.AbortTransaction();
                            Console.WriteLine("Error writing to MongoDB: " + e.Message);
                        }
                    }
                }

                return existingUsers;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return existingUsers;
            }
        }


        public async Task<bool> UpdateUser(User user)
        {
            var users = session.Client.GetDatabase("MyPerformance").GetCollection<User>("users");
            var filter = Builders<User>.Filter.Eq("Id", user.Id);
            var result = await users.ReplaceOneAsync(filter, user);
            return result.IsAcknowledged;
        }

        public async Task<List<User>> GetUsers()
        {
            var users = session.Client.GetDatabase("MyPerformance").GetCollection<User>("users");
            var filter = new FilterDefinitionBuilder<User>().Empty;
            var results = await users.Find<User>(filter).ToListAsync();
            return results;
        }


        public List<DateTime> GetWeek()
        {
            var dow = DateTime.Today.DayOfWeek;
            var offset = 0;
            switch (dow)
            {
                case DayOfWeek.Monday:
                    offset = 0;
                    break;
                case DayOfWeek.Tuesday:
                    offset = -1;
                    break;
                case DayOfWeek.Wednesday:
                    offset = -2;
                    break;
                case DayOfWeek.Thursday:
                    offset = -3;
                    break;
                case DayOfWeek.Friday:
                    offset = -4;
                    break;
                case DayOfWeek.Saturday:
                    offset = -5;
                    break;
                case DayOfWeek.Sunday:
                    offset = -6;
                    break;
            }

            var week = new List<DateTime>
            {
                DateTime.Today.AddDays(offset),
                DateTime.Today.AddDays(offset + 1),
                DateTime.Today.AddDays(offset + 2),
                DateTime.Today.AddDays(offset + 3),
                DateTime.Today.AddDays(offset + 4),
                DateTime.Today.AddDays(offset + 5),
                DateTime.Today.AddDays(offset + 6)
            };
            return week;
        }

    }
}
