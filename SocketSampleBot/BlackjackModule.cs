using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;

namespace SampleBot {
	public class BlackjackModule : InteractiveBase<SocketCommandContext> {
		private Random m_RNG;
		private List<Card> m_Deck = new List<Card>();
		private const int InitialChipCount = 200;

		private string GetDisplayName(IUser user) => (user as IGuildUser)?.Nickname ?? user.Username;

		private Criteria<SocketMessage> GetStandardCriteria(IEnumerable<IUser> players, ICriterion<SocketMessage> extraCriterion) =>
			new Criteria<SocketMessage>(
				new ORCriteria<SocketMessage>(players.Select(user => new EnsureFromUserCriterion(user.Id))),
				new EnsureSourceChannelCriterion()
			).AddCriterion(extraCriterion);

		[Command("play blackjack", RunMode = RunMode.Async)]
		public async Task PlayBlackjack(params IUser[] users) {
			if (users == null) {
				await ReplyAsync("Er moet minstens 1 andere speler zijn voor Blackjack.");
				return;
			}

			await ReplyAsync("At least one player needs to accept the invitation to join. They have 60 seconds to do so.");

			string[] positive = new[] { "yes", "join", "ok", "accept" };
			string[] negative = new[] { "no", "ignore", "deny", "refuse" };

			var joinedUsers = new HashSet<IUser>();
			await foreach (SocketMessage msg in WaitAllPlayers(users, GetStandardCriteria(users, new EnsureOneOfCriterion(new[] { positive, negative }.SelectMany(s => s))))) {
				if (positive.Contains(msg.Content.ToLower())) {
					joinedUsers.Add(msg.Author);
					await ReplyAsync("Confirmed: " + GetDisplayName(msg.Author) + " is in the game.");
				} else if (negative.Contains(msg.Content.ToLower())) {
					await ReplyAsync("Confirmed: " + GetDisplayName(msg.Author) + " is not playing.");
				}
			}
			if (joinedUsers.Count == 0) {
				await ReplyAsync("Can't play the game because nobody has accepted the invitation.");
			} else {
				await ReplyAsync("Let's play blackjack.");
				await PlayBlackjack(joinedUsers);
			}
		}

		private async IAsyncEnumerable<SocketMessage> WaitAllPlayers(IEnumerable<IUser> users, ICriterion<SocketMessage> criterion) {
			var usersRemaining = new HashSet<IUser>(users);
			SocketMessage result;
			while (usersRemaining.Count > 0 && (result = await NextMessageAsync(criterion, TimeSpan.FromSeconds(60))) != null) {
				yield return result;
			}
		}

		private void ShuffleDeck() {
			int n = m_Deck.Count;
			while (n > 1) {
				n--;
				int k = m_RNG.Next(n + 1);
				Card value = m_Deck[k];
				m_Deck[k] = m_Deck[n];
				m_Deck[n] = value;
			}
		}

		private Card DrawCard() {
			// TODO fix index error
			var ret = m_Deck[m_Deck.Count - 1];
			m_Deck.RemoveAt(m_Deck.Count - 1);
			return ret;
		}

		private async Task PlayBlackjack(IEnumerable<IUser> users) {
			m_RNG = new Random();

			m_Deck = new List<Card>(52);
			bool gameInProgress = true;

			var players = users.Select(user => new BlackjackPlayer(user, InitialChipCount) {
				PrivateCards = (DrawCard(), DrawCard())
			}).ToArray();

			while (gameInProgress) {
				m_Deck.Clear();
				for (int i = 0; i < 13; i++) {
					m_Deck.Add((Card) i);
					m_Deck.Add((Card) i);
					m_Deck.Add((Card) i);
					m_Deck.Add((Card) i);
				}
				ShuffleDeck();

				IEnumerable<BlackjackPlayer> playersInRound = players;
				int pot = 0;

				BlackjackPlayer getPlayer(IUser user) => playersInRound.First(player => player.DiscordUser == user);

				foreach (var player in players) {
					await (await player.DiscordUser.GetOrCreateDMChannelAsync()).SendMessageAsync("You were dealt: " + player.PrivateCards.ToString());
				}

				await ReplyAsync("All players were dealt their two starting cards. Who wants to bet?");

				async Task askBetAsync() {
					var betRegex = new Regex("(i )?bet (?<amount>[0-9]+)");
					await foreach (SocketMessage msg in WaitAllPlayers(playersInRound.Select(player => player.DiscordUser), new ORCriteria<SocketMessage>(
							new EnsureOneOfCriterion("fold", "i fold"),
							new RegexCriterion(betRegex)
						))) {
						BlackjackPlayer userAsPlayer = getPlayer(msg.Author);
						if (msg.Content.Contains("fold")) {
							await ReplyAsync("Confirmed: " + GetDisplayName(msg.Author) + " folds.");
							playersInRound = playersInRound.Where(player => player != userAsPlayer);
						} else {
							int newBetAmount = int.Parse(betRegex.Match(msg.Content).Groups["amount"].Value); // TODO fix potential errors
							string response = "";
							if (newBetAmount > userAsPlayer.ChipCount) {
								response = "That's more than you have, but I'll let you go all in.";
								newBetAmount = userAsPlayer.ChipCount;
							}
							userAsPlayer.ChipCount -= newBetAmount;
							response += "Confirmed: " + GetDisplayName(msg.Author);
							if (userAsPlayer.ChipCount == 0) {
								response += " goes all in.";
							} else {
								response += " bets " + newBetAmount + (userAsPlayer.BetAmount != 0
									? "on top of their existing bet of " + userAsPlayer.BetAmount + ", for a total bet of " + (userAsPlayer.BetAmount + newBetAmount)
									: "") + ".";
							}
							pot += newBetAmount;
							userAsPlayer.BetAmount = newBetAmount;
							response += " That makes the pot " + pot + " chips large.";

							await ReplyAsync(response);
						}
					}
				}
				await askBetAsync();

				// Who wants to hit?
				string response = "Everyone has made their move. ";
				foreach (BlackjackPlayer player in playersInRound) {
					await ReplyAsync(response + GetDisplayName(player.DiscordUser) + ", do you want to be hit, or pass?");
					response = "";
					SocketMessage msg;
					do {
						msg = await NextMessageAsync(new Criteria<SocketMessage>(new EnsureFromUserCriterion(player.DiscordUser.Id), new EnsureOneOfCriterion("hit", "hit me", "pass", "i pass")));
						if (msg.Content.StartsWith("hit")) {
							Card dealtCard = DrawCard();
							player.PublicCards.Add(dealtCard);
							await ReplyAsync("You were dealt: " + dealtCard);
						}
					} while (msg.Content.StartsWith("hit"));
				}

				await ReplyAsync("Everyone has made their move. Does anyone want to change their bets?");
				await askBetAsync();

				// Reveal cards
				response = "That concludes this round. These were the cards:";
				foreach (BlackjackPlayer player in playersInRound) {
					response += "\n" + GetDisplayName(player.DiscordUser) + ": " + string.Join(", ", new[] { player.PrivateCards.Item1, player.PrivateCards.Item2 }.Concat(player.PublicCards))
					 + ". That is a total value of " + player.Value + ".";
					if (player.Value > 21) {
						response += " That's greater than 21.";
					} else if (player.Value == 21) {
						response += " That's blackjack.";
					}
				}

				// Decide round winner
				BlackjackPlayer winner = playersInRound.Aggregate((BlackjackPlayer) null, (current, next) => {
					if (next.Value <= 21 && (current == null || next.Value > current.Value)) {
						return next;
					}
					return current;
				});
				if (winner == null) {
					response = "It appears there is no winner! Everyone gets their chips back.";
					foreach (BlackjackPlayer player in playersInRound) {
						player.ChipCount += player.BetAmount;
					}
				} else {
					winner.ChipCount += pot;
					response = GetDisplayName(winner.DiscordUser) + " wins this round and the pot of " + pot + ".\n";
					response += string.Join("\n", playersInRound.Where(player => player.ChipCount == 0).Select(player => GetDisplayName(player.DiscordUser) + " has lost this game."));
					
					if (playersInRound.Where(player => player.ChipCount == 21).Count() > 1) {
						response += "\nIt appears there was more than one player with a score of 21. This game is still in Beta™, so please forgive that I let the wrong player win.";
					}
				}
				foreach (BlackjackPlayer player in playersInRound) {
					player.BetAmount = 0;
				}
				players = players.Where(player => player.ChipCount > 0).ToArray();
				if (players.Length <= 1) {
					gameInProgress = false;
					response += "\nAnd with that, " + GetDisplayName(players[0].DiscordUser) + " is the only remaining player and wins the game.";
				}

				playersInRound = players;
				await ReplyAsync(response);
			}
		}
	}
}
