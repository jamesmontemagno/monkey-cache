namespace MonkeyCache
{
    public interface ILid
    {
		// Return null from either method to cancel the calling Barrel operation
		string AddingToBarrel(string content);
		string GettingFromBarrel(string content);
    }
}
