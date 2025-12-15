using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

public class MyCustomAuthStateProvider : AuthenticationStateProvider
{
	private ClaimsPrincipal _anonymous = new(new ClaimsIdentity());
	// _anonymous - falls _currentUser null ist
	private ClaimsPrincipal? _currentUser = null;


	public override Task<AuthenticationState> GetAuthenticationStateAsync()
	{
		return Task.FromResult(new AuthenticationState(_currentUser ?? _anonymous));
	}


	public void Login(string username)
	{
		ClaimsIdentity identity = new([new Claim(ClaimTypes.Name, username)],
				"MyCustomAuthType");  // von SPAA erfunden, keiner der Standardtypen

		_currentUser = new ClaimsPrincipal(identity);
		NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
	}


	public void Logout()
	{
		_currentUser = null;
		NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
	}
}

