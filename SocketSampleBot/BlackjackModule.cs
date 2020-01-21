using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;

namespace SampleBot {
	public class BlackjackModule : InteractiveBase<SocketCommandContext> {
		private Random m_RNG;
		private List<Card> m_Deck = new List<Card>();

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
					await ReplyAsync("Confirmed: " + ((msg.Author as IGuildUser)?.Nickname ?? msg.Author.Username) + " is in the game.");
				} else if (negative.Contains(msg.Content.ToLower())) {
					await ReplyAsync("Confirmed: " + ((msg.Author as IGuildUser)?.Nickname ?? msg.Author.Username) + " is not playing.");
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

			while (gameInProgress) {
				m_Deck.Clear();
				for (int i = 0; i < 13; i++) {
					m_Deck.Add((Card) i);
					m_Deck.Add((Card) i);
					m_Deck.Add((Card) i);
					m_Deck.Add((Card) i);
				}
				ShuffleDeck();

				var players = users.Select(user => new BlackjackPlayer(user, DrawCard(), DrawCard())).ToArray();
				foreach (var player in players) {
					await (await player.DiscordUser.GetOrCreateDMChannelAsync()).SendMessageAsync("You were dealt: " + player.PrivateCards.ToString());
				}

				// Who wants to bet?
				// Who wants to hit?
				// Who wants to bet?
				// Reveal cards
				// Decide round winner
				// If only one remaining: end loop
			}
		}
	}
}
