using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CartoonCharacters.Models;

namespace CartoonCharacters.Services;

public class ChartServices
{
    private readonly DatabaseServices _databaseServices;

    public ChartServices()
    {
        _databaseServices = new DatabaseServices();
    }

    public async Task<List<TopCharacter>> GetTop5ByRatingAsync()
    {
        var allCharacters = await _databaseServices.GetAllCharactersAsync();
        
        var top5 = allCharacters
            .OrderByDescending(c => c.Rating)
            .Take(5)
            .Select(c => new TopCharacter
            {
                Name = c.Name,
                Rating = c.Rating,
                RatingVotes = c.RatingVotes
            })
            .ToList();
        
        return top5;
    }
}