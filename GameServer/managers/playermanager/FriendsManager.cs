﻿using System.Reflection;
using log4net;
using Project.Database;
using Project.DataBase;
using Project.GS.Events;

namespace Project.GS.Friends;

	/// <summary>
	/// Game Player Friends List Manager
	///  - 친구데이터를 캐싱해서 사용하고, 케릭터로 월드에 진입하는 순간 캐시에 담아 놓는다.
	///  - ReaderWriterDictionary 대신에 레디스를 사용하는 방법도 고려해야 한다.
	/// </summary>
	public sealed class FriendsManager
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Players Indexed Friends Lists Cache.
		/// </summary>
		private ReaderWriterDictionary<GamePlayer, string[]> PlayersFriendsListsCache { get; set; }

		/// <summary>
		/// Players Indexed Friends Offline Status Cache.
		/// </summary>
		private ReaderWriterDictionary<GamePlayer, FriendStatus[]> PlayersFriendsStatusCache { get; set; }

		/// <summary>
		/// Server Database Reference.
		/// </summary>
		private IObjectDatabase Database { get; set; }

		/// <summary>
		/// Get this Player Friends List
		/// </summary>
		public string[] this[GamePlayer player]
		{
			get
			{
				if (player == null)
					return Array.Empty<string>();

				string[] result;
				return PlayersFriendsListsCache.TryGetValue(player, out result) ? result : Array.Empty<string>();
			}
		}

		/// <summary>
		/// Create a new Instance of <see cref="FriendsManager"/>
		/// </summary>
		public FriendsManager(IObjectDatabase Database)
		{
			this.Database = Database;
			PlayersFriendsListsCache = new ReaderWriterDictionary<GamePlayer, string[]>();
			PlayersFriendsStatusCache = new ReaderWriterDictionary<GamePlayer, FriendStatus[]>();
			GameEventMgr.AddHandler(GameClientEvent.StateChanged, OnClientStateChanged);
			GameEventMgr.AddHandler(GamePlayerEvent.GameEntered, OnPlayerGameEntered);
			GameEventMgr.AddHandler(GamePlayerEvent.Quit, OnPlayerQuit);
			GameEventMgr.AddHandler(GamePlayerEvent.ChangeAnonymous, OnPlayerChangeAnonymous);
		}

		/// <summary>
		/// Add Player to Friends Manager Cache
		/// </summary>
		/// <param name="Player">Gameplayer to Add</param>
		public void AddPlayerFriendsListToCache(GamePlayer Player)
		{
			if (Player == null)
				throw new ArgumentNullException("Player");

			var friends = Player.SerializedFriendsList;

			if (!PlayersFriendsListsCache.AddIfNotExists(Player, friends))
			{
				if (log.IsWarnEnabled)
					log.WarnFormat("Gameplayer ({0}) is already registered in Friends Manager Cache while adding!", Player);
			}

			var offlineFriends = Array.Empty<FriendStatus>();
			if (friends.Any())
			{
				offlineFriends = Database.SelectObjects<DOLCharacters>(DB.Column(nameof(DOLCharacters.Name)).IsIn(friends))
					.Select(chr => new FriendStatus(chr.Name, chr.Level, chr.Class, chr.LastPlayed)).ToArray();
			}

			if (!PlayersFriendsStatusCache.AddIfNotExists(Player, offlineFriends))
			{
				if (log.IsWarnEnabled)
					log.WarnFormat("Gameplayer ({0}) is already registered in Friends Manager Status Cache while adding!", Player);
			}

		}

		/// <summary>
		/// Remove Player from Friends Manager Cache
		/// </summary>
		/// <param name="Player">Gameplayer to Remove</param>
		public void RemovePlayerFriendsListFromCache(GamePlayer Player)
		{
			if (Player == null)
				throw new ArgumentNullException("Player");

			string[] friends;
			if (!PlayersFriendsListsCache.TryRemove(Player, out friends))
			{
				if (log.IsWarnEnabled)
					log.WarnFormat("Gameplayer ({0}) was not registered in Friends Manager Cache while trying to remove!", Player);
			}

			FriendStatus[] offlineFriends;
			if (!PlayersFriendsStatusCache.TryRemove(Player, out offlineFriends))
			{
				if (log.IsWarnEnabled)
					log.WarnFormat("Gameplayer ({0}) was not registered in Friends Manager Status Cache while trying to remove!", Player);
			}
		}

		/// <summary>
		/// Add a Friend Entry to GamePlayer Friends List.
		/// </summary>
		/// <param name="Player">GamePlayer to Add Friend to.</param>
		/// <param name="Friend">Friend's Name to Add</param>
		/// <returns>True if friend was added successfully</returns>
		public bool AddFriendToPlayerList(GamePlayer Player, string Friend)
		{
			if (Player == null)
				throw new ArgumentNullException("Player");
			if (Friend == null)
				throw new ArgumentNullException("Friend");

			Friend = Friend.Trim();

			if (string.IsNullOrEmpty(Friend))
				throw new ArgumentException("Friend need to be a valid non-empty or white space string!", "Friend");

			var success = false;
			if (!PlayersFriendsListsCache.TryUpdate(Player, list => {
				if (list.Contains(Friend))
					return list;

				success = true;
				return list.Concat(new[] { Friend }).ToArray();
			}))
			{
				if (log.IsWarnEnabled)
					log.WarnFormat("Gameplayer ({0}) was not registered in Friends Manager Cache while trying to Add a new Friend ({1})", Player, Friend);
			}

			if (success)
			{
				//Player.Out.SendAddFriends(new[] { Friend }); Todo. 패킷 전송은 다음에 
				Player.SerializedFriendsList = this[Player];

				var offlineFriend = Database.SelectObjects<DOLCharacters>(DB.Column(nameof(DOLCharacters.Name)).IsEqualTo(Friend)).FirstOrDefault();

				if (offlineFriend != null)
				{
					if (!PlayersFriendsStatusCache.TryUpdate(Player, list => list.Where(frd => frd.Name != Friend)
															 .Concat(new[] { new FriendStatus(offlineFriend.Name, offlineFriend.Level, offlineFriend.Class, offlineFriend.LastPlayed) })
															 .ToArray()))
					{
						if (log.IsWarnEnabled)
							log.WarnFormat("Gameplayer ({0}) was not registered in Friends Manager Status Cache while trying to Add a new Friend ({1})", Player, Friend);
					}
				}
			}

			return success;
		}

		/// <summary>
		/// Remove a Friend Entry from GamePlayer Friends List.
		/// </summary>
		/// <param name="Player">GamePlayer to Add Friend to.</param>
		/// <param name="Friend">Friend's Name to Add</param>
		/// <returns>True if friend was added successfully</returns>
		public bool RemoveFriendFromPlayerList(GamePlayer Player, string Friend)
		{
			if (Player == null)
				throw new ArgumentNullException("Player");
			if (Friend == null)
				throw new ArgumentNullException("Friend");

			Friend = Friend.Trim();

			if (string.IsNullOrEmpty(Friend))
				throw new ArgumentException("Friend need to be a valid non-empty or white space string!", "Friend");

			var success = false;
			if (!PlayersFriendsListsCache.TryUpdate(Player, list => {
				var result = list.Except(new[] { Friend }).ToArray();
				if (result.Length < list.Length)
				{
					success = true;
					return result;
				}

				return list;
			}))
			{
				if (log.IsWarnEnabled)
					log.WarnFormat("Gameplayer ({0}) was not registered in Friends Manager Cache while trying to Remove a Friend ({1})", Player, Friend);
			}

			if (success)
			{
				// Player.Out.SendRemoveFriends(new[] { Friend }); Todo. 패킷전송은 나중에
				Player.SerializedFriendsList = this[Player];

				if (!PlayersFriendsStatusCache.TryUpdate(Player, list => list.Where(frd => frd.Name != Friend).ToArray()))
				{
					if (log.IsWarnEnabled)
						log.WarnFormat("Gameplayer ({0}) was not registered in Friends Manager Status Cache while trying to Remove a Friend ({1})", Player, Friend);
				}
			}

			return success;
		}

		/// <summary>
		/// Send Players Friends List Snapshot
		/// </summary>
		/// <param name="Player">GamePlayer to Send Friends snapshot to</param>
		public void SendPlayerFriendsSnapshot(GamePlayer Player)
		{
			if (Player == null)
				throw new ArgumentNullException("Player");

			//Player.Out.SendCustomTextWindow("Friends (snapshot)", this[Player]); Todo. 패킷전송은 나중에
		}

		/// <summary>
		/// Send Players Friends Social Windows
		/// </summary>
		/// <param name="Player">GamePlayer to Send Friends social window to</param>
		public void SendPlayerFriendsSocial(GamePlayer Player)
		{
			if (Player == null)
				throw new ArgumentNullException("Player");

			// "TF" - clear friend list in social
			//Player.Out.SendMessage("TF", eChatType.CT_SocialInterface, eChatLoc.CL_SystemWindow); Todo. 패킷전송은 나중에

			var offlineFriends = this[Player].ToList();
			var index = 0;
			foreach (var friend in this[Player].Select(name => PlayersFriendsListsCache.FirstOrDefault(kv => kv.Key.Name == name))
					 .Where(kv => kv.Key != null && !kv.Key.IsAnonymous).Select(kv => kv.Key))
			{
				offlineFriends.Remove(friend.Name);
				/*
				Player.Out.SendMessage(string.Format("F,{0},{1},{2},{3},\"{4}\"",
					index++,
					friend.Name,
					friend.Level,
					friend.CharacterClass.ID,
					friend.CurrentZone == null ? string.Empty : friend.CurrentZone.Description),
					eChatType.CT_SocialInterface, eChatLoc.CL_SystemWindow);
					Todo. 패킷전송은 나중에
				*/
			}

			// Query Offline Characters
			FriendStatus[] offline;

			if (PlayersFriendsStatusCache.TryGetValue(Player, out offline))
			{
				foreach (var friend in offline.Where(frd => offlineFriends.Contains(frd.Name)))
				{
					/*
					Player.Out.SendMessage(string.Format("F,{0},{1},{2},{3},\"{4}\"",
						index++,
						friend.Name,
						friend.Level,
						friend.ClassID,
						friend.LastPlayed),
						eChatType.CT_SocialInterface, eChatLoc.CL_SystemWindow);
						Todo. 패킷전송은 나중에
					*/
				}
			}

		}

		/// <summary>
		/// Send Initial Player Friends List to Client
		/// </summary>
		/// <param name="Player">GamePlayer to send the list to.</param>
		private void SendPlayerFriendsList(GamePlayer Player)
		{
			/*
			Player.Out.SendAddFriends(this[Player].Where(name => {
				var player = PlayersFriendsListsCache.FirstOrDefault(kv => kv.Key != null && kv.Key.Name == name);
				return player.Key != null && !player.Key.IsAnonymous;
			}).ToArray());
			Todo. 패킷전송은 나중에
			*/
		}

		/// <summary>
		/// Notify Friends of this Player that he entered Game
		/// </summary>
		/// <param name="Player">GamePlayer to notify to friends</param>
		private void NotifyPlayerFriendsEnteringGame(GamePlayer Player)
		{
			var playerName = Player.Name;
			var playerUpdate = new[] { playerName };

			foreach (GamePlayer friend in PlayersFriendsListsCache.Where(kv => kv.Value.Contains(playerName)).Select(kv => kv.Key))
			{
				//friend.Out.SendAddFriends(playerUpdate); Todo. 패킷전송은 나중에
			}
		}

		/// <summary>
		/// Notify Friends of this Player that he exited Game
		/// </summary>
		/// <param name="Player">GamePlayer to notify to friends</param>
		private void NotifyPlayerFriendsExitingGame(GamePlayer Player)
		{
			var playerName = Player.Name;
			var playerUpdate = new[] { playerName };

			foreach (GamePlayer friend in PlayersFriendsListsCache.Where(kv => kv.Value.Contains(playerName)).Select(kv => kv.Key))
			{
				//friend.Out.SendRemoveFriends(playerUpdate); Todo. 패킷전송은 나중에
			}

			var offline = new FriendStatus(Player.Name, Player.Level, 0 /* Player.CharacterClass.ID Todo. 패킷전송 */ , DateTime.Now);

			PlayersFriendsStatusCache.FreezeWhile(dict => {
				foreach (var list in dict.Where(kv => kv.Value.Any(frd => frd.Name == Player.Name)).ToArray())
					dict[list.Key] = list.Value.Where(frd => frd.Name != Player.Name).Concat(new[] { offline }).ToArray();
			});
		}

		/// <summary>
		/// Trigger Player Friend List Update on World Enter
		/// </summary>
		private void OnClientStateChanged(DOLEvent e, object sender, EventArgs arguments)
		{
			var client = sender as GameClient;
			if (client == null)
				return;

			if (client.ClientState == GameClient.eClientState.WorldEnter && client.Player != null)
			{
				// Load Friend List
				AddPlayerFriendsListToCache(client.Player);
				SendPlayerFriendsList(client.Player);
			}
		}

		/// <summary>
		/// Trigger Player's Friends Notice on Game Enter
		/// </summary>
		private void OnPlayerGameEntered(DOLEvent e, object sender, EventArgs arguments)
		{
			var player = sender as GamePlayer;
			if (player == null)
				return;

			if (!player.IsAnonymous)
				NotifyPlayerFriendsEnteringGame(player);
		}

		/// <summary>
		/// Trigger Player's Friends Notice on Game Leave, And Cleanup Player Friend List
		/// </summary>
		private void OnPlayerQuit(DOLEvent e, object sender, EventArgs arguments)
		{
			var player = sender as GamePlayer;
			if (player == null)
				return;

			RemovePlayerFriendsListFromCache(player);
			if (!player.IsAnonymous)
				NotifyPlayerFriendsExitingGame(player);
		}

		/// <summary>
		/// Trigger Player's Friends Notice on Anonymous State Change
		/// </summary>
		private void OnPlayerChangeAnonymous(DOLEvent e, object sender, EventArgs arguments)
		{
			var player = sender as GamePlayer;
			if (player == null)
				return;

			if (player.IsAnonymous)
				NotifyPlayerFriendsExitingGame(player);
			else
				NotifyPlayerFriendsEnteringGame(player);
		}
	}