﻿using Microcube.Game.Blocks.Enums;
using Microcube.Graphics.ColorModels;
using Silk.NET.Maths;

namespace Microcube.Game.Blocks
{
    /// <summary>
    /// Represents a player that will be controlled by a... player. I mean who will play this game.
    /// </summary>
    public class Player : Block
    {
        private readonly Level level;

        private PlayerState state = PlayerState.Falling;
        private PlayerBarrier barrier = PlayerBarrier.Nothing;

        private float innerOffset = 0.0f;
        private float velocity = 0.0f;

        private bool isKeyPressed = false;
        private bool isReversed = false;
        private bool changeAxis = false;
        private bool isPushed = false;

        private readonly float mass = 0.01f;
        private readonly float gravity = -9.81f;

        public override Vector3D<float> Position
        {
            get => base.Position;
            set
            {
                base.Position = value;
                // TODO: something is wrong when player falling and pushing my other block
                ModelMatrix = CalculateMovingMatrix(innerOffset, barrier, changeAxis)
                    * Matrix4X4.CreateTranslation(Position);
            }
        }

        public override bool IsRender
        {
            get => base.IsRender && !level.IsFinished;
            private protected set => base.IsRender = value;
        }

        /// <summary>
        /// Position what will be setted when player will be died or a level will be started.
        /// </summary>
        public Vector3D<float> StartPosition { get; set; }

        /// <summary>
        /// Fake position of the player that takes into account it's inner offset
        /// </summary>
        public Vector3D<float> OffsettedPosition
        {
            get
            {
                switch (state)
                {
                    case PlayerState.Moving:
                        if (barrier == PlayerBarrier.Nothing)
                        {
                            float moveX = 0.0f, moveZ = innerOffset;
                            if (changeAxis)
                                (moveX, moveZ) = (moveZ, moveX);

                            return Position + new Vector3D<float>(moveX, 0.0f, moveZ);
                        }
                        else
                        {
                            Vector4D<float> vector4D = new Vector4D<float>(0.0f, 0.0f, 0.0f, 1.0f)
                                * CalculateMovingMatrix(innerOffset, barrier, changeAxis);

                            return Position + new Vector3D<float>(vector4D.X, vector4D.Y, vector4D.Z);
                        }
                    case PlayerState.Falling:
                        return new Vector3D<float>(Position.X, Position.Y + innerOffset, Position.Z);
                }
                return Position;
            }
        }

        /// <summary>
        /// Next position that's expected when player will do step.
        /// </summary>
        public Vector3D<float> NextPosition
        {
            get
            {
                if (state == PlayerState.Moving && innerOffset != 0.0f)
                {
                    float moveX = 0.0f;
                    float moveY = barrier == PlayerBarrier.Step || barrier == PlayerBarrier.Wall ? 1.0f : 0.0f;
                    float moveZ = barrier != PlayerBarrier.Wall ? MathF.CopySign(1.0f, innerOffset) : 0.0f;

                    if (changeAxis)
                        (moveX, moveZ) = (moveZ, moveX);

                    return new Vector3D<float>(Position.X + moveX, Position.Y + moveY, Position.Z + moveZ);
                }
                return Position;
            }
        }

        // TODO: add acceleration and limit by max energy
        /// <summary>
        /// Energy of the block, I would say it represents a player speed.
        /// </summary>
        public float Energy { get; set; }

        public Player(Vector3D<float> startPosition, RgbaColor color, Level level) : base(startPosition, color)
        {
            ArgumentNullException.ThrowIfNull(level, nameof(level));
            this.level = level;

            StartPosition = startPosition;
            Energy = 1.5f;
        }

        /// <summary>
        /// When it called, player is moving if it isn't blocked by the state or something like that.
        /// </summary>
        /// <param name="isReversed">Is change direction from forward to otherwise.</param>
        /// <param name="changeAxis">Is change axis from Z to X.</param>
        public void Move(bool isReversed, bool changeAxis)
        {
            if (state != PlayerState.Falling)
            {
                this.isReversed = isReversed;
                if (innerOffset == 0.0f)
                    this.changeAxis = changeAxis;

                if (this.changeAxis == changeAxis && barrier != PlayerBarrier.Unsuitable && barrier != PlayerBarrier.Trap)
                    isKeyPressed = true;
            }
        }

        /// <summary>
        /// Just pushes player, it's not the same to change position.
        /// It should be used when different block pushes this player.
        /// </summary>
        /// <param name="offset">Offset of the player.</param>
        public void Push(Vector3D<float> offset)
        {
            Position += offset;
            isPushed = true;
        }

        public void Update(float deltaTime)
        {
            // TODO: something is wrong with velocity on different framerates
            velocity += velocity * (mass * gravity) * deltaTime;
            Color = (RgbaColor)((HsvaColor)Color).OffsetHue(Energy * 240.0f * deltaTime);

            if (state == PlayerState.Falling)
            {
                velocity -= deltaTime;
                innerOffset += velocity;

                if (innerOffset < -10.0f)
                    ProcessPosition(StartPosition);
                else
                {
                    Block? highestBlock = level.GetHighestBarrierFromHeight(Position.X, Position.Z, Position.Y);
                    if (highestBlock?.IsBarrier is true && OffsettedPosition.Y - highestBlock.Position.Y < 1.0f)
                        ProcessPosition(new Vector3D<float>(Position.X, highestBlock.Position.Y + 1.0f, Position.Z));
                }

                ModelMatrix = Matrix4X4.CreateTranslation(Position.X, Position.Y + innerOffset, Position.Z);
            }
            else
            {
                if (innerOffset == 0.0f && state == PlayerState.Standing)
                    barrier = EnvirnomentAnalysis.GetGlobalBarrierFromPosition(Position, level.GetBarrierBlockCoordinates(), isReversed, changeAxis);

                // TODO: rewrite it
                if (!isPushed && state == PlayerState.Standing && (Math.Truncate(Position.X) != 0.0f || Math.Truncate(Position.Y) != 0.0f))
                    ProcessPosition(new Vector3D<float>(MathF.Round(Position.X), Position.Y, MathF.Round(Position.Z)));

                float movingForceStrength = velocity == 0.0f ? Energy : 0.50f;
                float weightForceStrength = 0.25f;

                if (barrier == PlayerBarrier.Nothing || barrier == PlayerBarrier.Step || barrier == PlayerBarrier.Wall)
                {
                    if (isKeyPressed)
                    {
                        float movingForce = (isReversed ? -movingForceStrength : movingForceStrength) * deltaTime;
                        velocity += movingForce;
                    }

                    if (innerOffset != 0.0f)
                    {
                        float weightForce = barrier switch
                        {
                            PlayerBarrier.Nothing => (MathF.Abs(innerOffset) > 0.5f ? weightForceStrength : -weightForceStrength) * MathF.Sign(innerOffset),
                            PlayerBarrier.Step => (MathF.Abs(innerOffset) < 1.5f ? -weightForceStrength : weightForceStrength) * MathF.Sign(innerOffset),
                            PlayerBarrier.Wall => MathF.CopySign(weightForceStrength, -innerOffset),
                            _ => 0.0f
                        } * deltaTime;

                        velocity += weightForce;
                    }
                }

                float previousInnerOffset = innerOffset;
                innerOffset += velocity;

                if (state == PlayerState.Moving && MathF.Sign(previousInnerOffset) != MathF.Sign(innerOffset))
                    ProcessPosition(Position);
                else
                {
                    if (innerOffset != 0.0f)
                        state = PlayerState.Moving;

                    float criticalOffset = barrier == PlayerBarrier.Step ? 2.0f : 1.0f;
                    if (MathF.Abs(innerOffset) > criticalOffset)
                        ProcessPosition(NextPosition);
                }

                ModelMatrix = CalculateMovingMatrix(innerOffset, barrier, changeAxis)
                    * Matrix4X4.CreateTranslation(Position);
            }

            isKeyPressed = false;
            isPushed = false;
        }

        /// <summary>
        /// Processes new position like when player was stepped to this position.
        /// </summary>
        /// <param name="position">New position.</param>
        public void ProcessPosition(Vector3D<float> position)
        {
            innerOffset = 0.0f;
            velocity = 0.0f;
            state = PlayerState.Standing;

            Position = position;

            Block? highestBlock = level.GetHighestBarrierFromHeight(Position.X, Position.Z, Position.Y);
            if (highestBlock == null || (highestBlock.IsBarrier && OffsettedPosition.Y - highestBlock.Position.Y > 1.0f))
                state = PlayerState.Falling;
        }

        /// <summary>
        /// Get player model matrix when it's moving forward.
        /// </summary>
        /// <param name="offset">Inner offset of the player.</param>
        /// <param name="barrier">A barrier forward player.</param>
        /// <param name="changeAxis">Is change axis from Z to X.</param>
        /// <returns>Player model matrix</returns>
        public static Matrix4X4<float> CalculateMovingMatrix(float offset, PlayerBarrier barrier, bool changeAxis)
        {
            float translateY = 0.5f, translateZ = -MathF.CopySign(0.5f, offset);

            if (barrier == PlayerBarrier.Wall || barrier == PlayerBarrier.Step)
            {
                (translateY, translateZ) = (translateZ, -translateY);
                if (offset < 0.0f)
                {
                    translateY = -translateY;
                    translateZ = -translateZ;
                }
            }

            Matrix4X4<float> matrix = Matrix4X4.CreateTranslation(0.0f, translateY, translateZ)
                * Matrix4X4.CreateRotationX(offset * (MathF.PI / 2.0f))
                * Matrix4X4.CreateTranslation(0.0f, -translateY, -translateZ);

            if (changeAxis)
                matrix *= Matrix4X4.CreateRotationY(MathF.PI / 2.0f);

            return matrix;
        }
    }
}