using HajurKoCarRental.Data;
using HajurKoCarRental.Models;
using Microsoft.EntityFrameworkCore;

namespace HajurKoCarRental.Services
{
    /// <summary>
    /// Implements User-Based Collaborative Filtering using Cosine Similarity
    /// to recommend cars to a logged-in customer based on rental history.
    ///
    /// Mathematical Formula:
    ///   similarity(u, v) = (R[u] · R[v]) / (||R[u]|| × ||R[v]||)
    ///
    ///   P(u, c) = Σ [ sim(u,v) × R[v][c] ] / Σ |sim(u,v)|
    ///
    /// Where:
    ///   R[u] = rental vector of user u (1 if rented car c, else 0)
    ///   sim  = cosine similarity score between two users
    ///   P    = predicted preference score for car c by user u
    /// </summary>
    public class RecommendationService
    {
        private readonly HajurKoCarRentalContext _context;

        public RecommendationService(HajurKoCarRentalContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Returns top N recommended cars for the given user.
        /// Falls back to most popular cars if user has no rental history.
        /// </summary>
        public async Task<List<Car>> GetRecommendedCarsAsync(string userId, int topN = 3)
        {
            // Step 1: Load all rental data (userId -> list of carIds rented)
            var allRentals = await _context.Rent!
                .Select(r => new { r.UserId, r.CarId })
                .ToListAsync();

            // Also include rental history
            var allHistory = await _context.RentalHistory!
                .Select(r => new { r.UserId, CarId = r.carId })
                .ToListAsync();

            // Merge both sources into one user->car dictionary
            var userCarMap = new Dictionary<string, HashSet<Guid>>();

            foreach (var r in allRentals.Concat(allHistory.Select(h => new { h.UserId, h.CarId })))
            {
                if (!userCarMap.ContainsKey(r.UserId))
                    userCarMap[r.UserId] = new HashSet<Guid>();
                userCarMap[r.UserId].Add(r.CarId);
            }

            // Step 2: Get all available cars
            var allCars = await _context.Car!
                .Include(c => c.CarBrand)
                .Where(c => c.Available == true && (c.IsCarBooked == false || c.IsCarBooked == null))
                .ToListAsync();

            // Step 3: If current user has no rental history → fallback to popularity
            if (!userCarMap.ContainsKey(userId) || userCarMap[userId].Count == 0)
            {
                return GetPopularCars(allCars, topN);
            }

            var currentUserCars = userCarMap[userId];

            // Step 4: Build all unique car IDs across all users
            var allCarIds = userCarMap.Values
                .SelectMany(v => v)
                .Distinct()
                .ToList();

            // Step 5: Compute cosine similarity between current user and all other users
            // sim(u,v) = (R[u]·R[v]) / (||R[u]|| × ||R[v]||)
            var similarityScores = new Dictionary<string, double>();

            foreach (var otherUserId in userCarMap.Keys)
            {
                if (otherUserId == userId) continue;

                var otherUserCars = userCarMap[otherUserId];

                // Dot product: number of cars both users rented
                double dotProduct = currentUserCars.Intersect(otherUserCars).Count();

                if (dotProduct == 0) continue;

                // Magnitudes: ||R[u]|| = sqrt(count of cars rented by u)
                double magnitudeU = Math.Sqrt(currentUserCars.Count);
                double magnitudeV = Math.Sqrt(otherUserCars.Count);

                // Cosine similarity
                double similarity = dotProduct / (magnitudeU * magnitudeV);
                similarityScores[otherUserId] = similarity;
            }

            // Step 6: If no similar users found → fallback to popularity
            if (similarityScores.Count == 0)
            {
                var availableForFallback = allCars.Where(c => !currentUserCars.Contains(c.Id)).ToList();
                return GetPopularCars(availableForFallback, topN);
            }

            // Step 7: Compute prediction score for each car not yet rented by current user
            // P(u,c) = Σ[sim(u,v) × R[v][c]] / Σ|sim(u,v)|
            var carScores = new Dictionary<Guid, double>();
            double totalSimilarity = similarityScores.Values.Sum();

            foreach (var carId in allCarIds)
            {
                // Skip cars already rented by current user
                if (currentUserCars.Contains(carId)) continue;

                double weightedSum = 0;
                foreach (var kvp in similarityScores)
                {
                    var otherUserId = kvp.Key;
                    var sim = kvp.Value;

                    if (userCarMap[otherUserId].Contains(carId))
                        weightedSum += sim * 1.0; // rating = 1 (binary: rented or not)
                }

                if (weightedSum > 0)
                    carScores[carId] = weightedSum / totalSimilarity;
            }

            // Step 8: Sort by score descending, pick top N available cars
            var recommendedCarIds = carScores
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();

            var recommendedCars = allCars
                .Where(c => recommendedCarIds.Contains(c.Id))
                .OrderByDescending(c => carScores.ContainsKey(c.Id) ? carScores[c.Id] : 0)
                .Take(topN)
                .ToList();

            // Step 9: If not enough recommendations, fill with popular cars
            if (recommendedCars.Count < topN)
            {
                var popularFallback = GetPopularCars(
                    allCars.Where(c => !recommendedCars.Select(r => r.Id).Contains(c.Id) && !currentUserCars.Contains(c.Id)).ToList(),
                    topN - recommendedCars.Count
                );
                recommendedCars.AddRange(popularFallback);
            }

            return recommendedCars;
        }

        /// <summary>
        /// Fallback: returns most rented cars across all users from Rent + RentalHistory tables.
        /// Returns empty list if no rentals exist in the system yet.
        /// </summary>
        private List<Car> GetPopularCars(List<Car> availableCars, int topN)
        {
            // Count how many times each car was rented across all users
            var rentCounts = _context.Rent!
                .GroupBy(r => r.CarId)
                .Select(g => new { CarId = g.Key, Count = g.Count() })
                .ToList();

            var historyCounts = _context.RentalHistory!
                .GroupBy(r => r.carId)
                .Select(g => new { CarId = g.Key, Count = g.Count() })
                .ToList();

            // Merge both counts
            var totalCounts = rentCounts
                .Concat(historyCounts)
                .GroupBy(x => x.CarId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Count));

            // If no car has ever been rented, return empty — don't show random cars
            if (totalCounts.Count == 0)
                return new List<Car>();

            // Only return cars that have been rented at least once, sorted by count
            return availableCars
                .Where(c => totalCounts.ContainsKey(c.Id) && totalCounts[c.Id] > 0)
                .OrderByDescending(c => totalCounts[c.Id])
                .Take(topN)
                .ToList();
        }
    }
}
