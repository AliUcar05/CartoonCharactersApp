using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using CartoonCharacters.Models;

namespace CartoonCharacters.Services;

public class DatabaseServices
{
    private readonly IMongoCollection<CartoonCharacter> _cartoonCharacters;
    private readonly IMongoCollection<UserProfile> _userProfiles;
    private readonly HashingService _hashingService = new();

    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.IgnoreCase
    );

    public DatabaseServices()
    {
        const string connectionUri = "mongodb://Meeeee:IAmTheBest@185.157.245.38:443/?authSource=admin";

        var settings = MongoClientSettings.FromConnectionString(connectionUri);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
        settings.SocketTimeout = TimeSpan.FromSeconds(10);

        var client = new MongoClient(settings);
        var database = client.GetDatabase("CartoonCharactersDB");

        _cartoonCharacters = database.GetCollection<CartoonCharacter>("CartoonCharactersCollection");
        _userProfiles = database.GetCollection<UserProfile>("UserProfiles");
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            await _cartoonCharacters.Database.RunCommandAsync<BsonDocument>(
                new BsonDocument("ping", 1));

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> ReplaceAllCharactersAsync(List<CartoonCharacter>? characters)
    {
        try
        {
            if (characters == null)
                return false;

            foreach (var character in characters)
            {
                if (string.IsNullOrWhiteSpace(character.Id))
                    character.Id = ObjectId.GenerateNewId().ToString();
            }

            await _cartoonCharacters.DeleteManyAsync(
                Builders<CartoonCharacter>.Filter.Empty);

            if (characters.Count > 0)
            {
                await _cartoonCharacters.InsertManyAsync(characters);
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> InsertUserProfileAsync(UserProfile? userProfile)
    {
        try
        {
            if (userProfile == null)
                return false;

            if (string.IsNullOrWhiteSpace(userProfile.Id))
                userProfile.Id = ObjectId.GenerateNewId().ToString();

            userProfile.UserName = userProfile.UserName.Trim();
            userProfile.Email = userProfile.Email.Trim().ToLowerInvariant();
            userProfile.FirstName = userProfile.FirstName.Trim();
            userProfile.LastName = userProfile.LastName.Trim();

            if (!IsValidEmail(userProfile.Email))
                return false;

            userProfile.Password = _hashingService.Encrypt(userProfile.Password);

            await _userProfiles.InsertOneAsync(userProfile);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> UpdateUserProfileAsync(UserProfile? userProfile)
    {
        try
        {
            if (userProfile == null)
                return false;

            if (string.IsNullOrWhiteSpace(userProfile.Id))
                return false;

            var filter = Builders<UserProfile>.Filter.Eq(u => u.Id, userProfile.Id);

            var oldUser = await _userProfiles.Find(filter).FirstOrDefaultAsync();

            if (oldUser == null)
                return false;

            userProfile.UserName = userProfile.UserName.Trim();
            userProfile.FirstName = userProfile.FirstName.Trim();
            userProfile.LastName = userProfile.LastName.Trim();
            userProfile.Email = userProfile.Email.Trim().ToLowerInvariant();

            if (!IsValidEmail(userProfile.Email))
                return false;

            if (string.IsNullOrWhiteSpace(userProfile.Password))
            {
                userProfile.Password = oldUser.Password;
            }
            else if (userProfile.Password == oldUser.Password)
            {
                userProfile.Password = oldUser.Password;
            }
            else
            {
                userProfile.Password = _hashingService.Encrypt(userProfile.Password.Trim());
            }

            var result = await _userProfiles.ReplaceOneAsync(filter, userProfile);

            return result.IsAcknowledged;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> DeleteUserProfileAsync(string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
                return false;

            var filter = Builders<UserProfile>.Filter.Eq(u => u.Id, userId);
            var result = await _userProfiles.DeleteOneAsync(filter);

            return result.DeletedCount > 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<UserProfile?> GetUserByIdAsync(string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
                return null;

            var filter = Builders<UserProfile>.Filter.Eq(u => u.Id, userId);
            return await _userProfiles.Find(filter).FirstOrDefaultAsync();
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<bool> UserExistsAsync(string userName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userName))
                return false;

            var normalizedUserName = userName.Trim();
            var filter = Builders<UserProfile>.Filter.Eq(u => u.UserName, normalizedUserName);

            return await _userProfiles.Find(filter).AnyAsync();
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var normalizedEmail = email.Trim().ToLowerInvariant();

            if (!IsValidEmail(normalizedEmail))
                return false;

            var filter = Builders<UserProfile>.Filter.Eq(u => u.Email, normalizedEmail);

            return await _userProfiles.Find(filter).AnyAsync();
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> UserNameExistsForAnotherUserAsync(string userName, string currentUserId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userName))
                return false;

            var normalizedUserName = userName.Trim();

            var filter = Builders<UserProfile>.Filter.And(
                Builders<UserProfile>.Filter.Eq(u => u.UserName, normalizedUserName),
                Builders<UserProfile>.Filter.Ne(u => u.Id, currentUserId)
            );

            return await _userProfiles.Find(filter).AnyAsync();
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> EmailExistsForAnotherUserAsync(string email, string currentUserId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var normalizedEmail = email.Trim().ToLowerInvariant();

            if (!IsValidEmail(normalizedEmail))
                return false;

            var filter = Builders<UserProfile>.Filter.And(
                Builders<UserProfile>.Filter.Eq(u => u.Email, normalizedEmail),
                Builders<UserProfile>.Filter.Ne(u => u.Id, currentUserId)
            );

            return await _userProfiles.Find(filter).AnyAsync();
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<List<UserProfile>> GetAllUserProfilesAsync()
    {
        try
        {
            return await _userProfiles
                .Find(Builders<UserProfile>.Filter.Empty)
                .ToListAsync();
        }
        catch (Exception)
        {
            return new List<UserProfile>();
        }
    }

    public async Task<int> CountAdminsAsync()
    {
        try
        {
            var filter = Builders<UserProfile>.Filter.Eq(u => u.IsAdmin, true);
            var count = await _userProfiles.CountDocumentsAsync(filter);

            return (int)count;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    public async Task<UserProfile?> AuthenticateUserAsync(string userName, string password)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                return null;

            var normalizedUserName = userName.Trim();

            var filter = Builders<UserProfile>.Filter.Eq(
                u => u.UserName,
                normalizedUserName
            );

            var user = await _userProfiles.Find(filter).FirstOrDefaultAsync();

            if (user == null)
                return null;

            var decryptedPassword = _hashingService.Decrypt(user.Password);

            if (decryptedPassword == password.Trim())
                return user;

            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<List<CartoonCharacter>> GetAllCharactersAsync()
    {
        try
        {
            return await _cartoonCharacters
                .Find(Builders<CartoonCharacter>.Filter.Empty)
                .ToListAsync();
        }
        catch (Exception)
        {
            return new List<CartoonCharacter>();
        }
    }

    private static bool IsValidEmail(string email)
    {
        return EmailRegex.IsMatch(email);
    }
}