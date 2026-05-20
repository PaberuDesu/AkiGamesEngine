using System;
using Microsoft.Xna.Framework;

namespace AkiGames.Scripts
{
    internal sealed class FactoryCreature
    {
        private const float RabbitFleeDistance = 3.6f;

        private readonly Random _random;
        private Vector2 _direction;
        private float _decisionTimer;

        public FactoryCreature(FactoryCreatureType creatureType, Vector2 position, Random random)
        {
            CreatureType = creatureType;
            Position = position;
            _random = random;
            ResetDecisionTimer();
        }

        public FactoryCreatureType CreatureType { get; }
        public Vector2 Position { get; private set; }
        public Point Tile => new((int)Math.Floor(Position.X), (int)Math.Floor(Position.Y));
        public bool BlocksMovement => CreatureType == FactoryCreatureType.Rabbit;

        public void Update(float dt, FactoryWorld world)
        {
            if (CreatureType == FactoryCreatureType.Rabbit && TryFleeFromPlayer(dt, world))
                return;

            _decisionTimer -= dt;
            if (_decisionTimer <= 0f)
                PickNextDirection();

            if (_direction == Vector2.Zero) return;

            float speed = CreatureType == FactoryCreatureType.Rabbit ? 1.15f : 0.8f;
            Vector2 delta = _direction * speed * dt;

            if (!TryMove(world, new Vector2(Position.X + delta.X, Position.Y)))
                PickNextDirection();

            if (!TryMove(world, new Vector2(Position.X, Position.Y + delta.Y)))
                PickNextDirection();
        }

        private bool TryFleeFromPlayer(float dt, FactoryWorld world)
        {
            if (world.Player.Level != FactoryLevel.Surface)
                return false;

            Vector2 away = Position - world.Player.Position;
            if (away.LengthSquared() > RabbitFleeDistance * RabbitFleeDistance)
                return false;

            if (away == Vector2.Zero)
                away = new Vector2((float)_random.NextDouble() - 0.5f, (float)_random.NextDouble() - 0.5f);

            away.Normalize();

            Vector2[] candidates =
            [
                away,
                new Vector2(away.Y, -away.X),
                new Vector2(-away.Y, away.X)
            ];

            float speed = 2.35f;
            for (int i = 0; i < candidates.Length; i++)
            {
                Vector2 direction = candidates[i];
                if (direction == Vector2.Zero) continue;
                direction.Normalize();

                Vector2 moved = new(Position.X + direction.X * speed * dt, Position.Y + direction.Y * speed * dt);
                if (!TryMove(world, new Vector2(moved.X, Position.Y))) continue;
                if (!TryMove(world, new Vector2(Position.X, moved.Y))) continue;

                _direction = direction;
                _decisionTimer = 0.18f;
                return true;
            }

            return false;
        }

        private bool TryMove(FactoryWorld world, Vector2 destination)
        {
            Point destinationTile = new((int)Math.Floor(destination.X), (int)Math.Floor(destination.Y));
            if (!world.CanCreatureStandAt(this, destinationTile))
                return false;

            Position = destination;
            return true;
        }

        private void PickNextDirection()
        {
            ResetDecisionTimer();

            if (_random.NextDouble() < 0.22)
            {
                _direction = Vector2.Zero;
                return;
            }

            Vector2[] candidates =
            [
                new Vector2(1f, 0f),
                new Vector2(-1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(0f, -1f),
                new Vector2(1f, 1f),
                new Vector2(-1f, 1f),
                new Vector2(1f, -1f),
                new Vector2(-1f, -1f)
            ];

            _direction = candidates[_random.Next(candidates.Length)];
            if (_direction != Vector2.Zero)
                _direction.Normalize();
        }

        private void ResetDecisionTimer() =>
            _decisionTimer = 0.7f + (float)_random.NextDouble() * 1.6f;
    }
}
