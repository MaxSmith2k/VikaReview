namespace Terrasoft.Configuration
{
	using System;
	using System.Net;
	using System.IO;
	using System.ServiceModel;
	using System.ServiceModel.Web;
	using System.ServiceModel.Activation;
	using System.Web;
	using Terrasoft.Core;
	using Newtonsoft.Json;
	
	[ServiceContract]
	[AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
	public class AnPokemonService
	{
		private UserConnection userConnection;
		private UserConnection UserConnection
		{
			get
			{
				return userConnection ?? (userConnection = HttpContext.Current.Session["UserConnection"] as UserConnection);
			}
		}
		#region constructors
		public AnPokemonService()
		{
		}
		public AnPokemonService(UserConnection userConnection)
		{
			this.userConnection = userConnection;
		}
		#endregion
		#region public methods
		/// <summary>
		/// method to add new pokemon
		/// </summary>
		/// <param name="name">name of the pokemon entered by user</param>	
		[OperationContract]
		[WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Wrapped, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
		public string AddPokemon(string name)
		{
			string result = getPokemonJSON(name);
			if (result == string.Empty) return "error";
			AnPokemonProxy pokemon = JsonConvert.DeserializeObject<AnPokemonProxy>(result);
			AnServiceHelper pokemonHelper = new AnServiceHelper(UserConnection, "AnPokemons", "AnName", pokemon.Name);
			if (pokemonHelper.isAdded()) return "exists";
			pokemonHelper.setImageId(pokemon);
			AnServiceHelper helper = new AnServiceHelper(UserConnection, "AnLookup_PokemonType", "Name", pokemon.Type);
			helper.insert();
			pokemon.TypeId = helper.getId();
			pokemonHelper.insert(pokemon);
			pokemon.Id = pokemonHelper.getId();
			foreach (string move in pokemon.Moves)
			{
				helper = new AnServiceHelper(UserConnection, "AnLookup_MoveType", "Name", move);
				helper.insert();
				string moveId = helper.getId();
				helper.insert("AnPokemonMove", "AnMoveId", moveId, "AnPokemonId", pokemon.Id);
			}
			return "ok";
		}
		#endregion
		#region private methods
		/// <summary>
		/// method to get JSON string from pokeapi
		/// </summary>
		/// <param name="name">name of the pokemon entered by user</param>	
		private string getPokemonJSON(string name)
		{
			try
			{
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://pokeapi.co/api/v2/pokemon/" + name);
				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
				string result = string.Empty;
				using (Stream dataStream = response.GetResponseStream())
				{
					StreamReader reader = new StreamReader(dataStream);
					result = reader.ReadToEnd();
				}
				return result;
			}
			catch (Exception e) { return string.Empty; }
		}
		#endregion
	}
}