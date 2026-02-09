public class VoteSubmission
{
    public string Token { get; set; } = "";
    public List<int> Favorites { get; set; } = new();
    public int? SuperFavorite { get; set; }
}
