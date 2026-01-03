using System;
using DLS.Description;
using DLS.Simulation;
using Seb.Helpers;
using Seb.Types;
using UnityEngine;
using static DLS.Graphics.DrawSettings;

namespace DLS.Game
{
	public class DevPinInstance : IMoveable
	{
		public readonly PinBitCount BitCount;
		public readonly char[] decimalDisplayCharBuffer = new char[16];

		// Size/Layout info
		public readonly Vector2 faceDir;

		public readonly bool IsInputPin;
		public readonly string Name;
		public readonly PinInstance Pin;
		public readonly Vector2Int StateGridDimensions;
		public readonly Vector2 StateGridSize;

		public PinValueDisplayMode pinValueDisplayMode;

		public DevPinInstance(PinDescription pinDescription, bool isInput)
		{
			Name = pinDescription.Name;
			ID = pinDescription.ID;
			IsInputPin = isInput;
			Position = pinDescription.Position;
			BitCount = pinDescription.BitCount;

			Pin = new PinInstance(pinDescription, new PinAddress(ID, 0), this, isInput);
			pinValueDisplayMode = pinDescription.ValueDisplayMode;

			// Calculate layout info
			faceDir = new Vector2(IsInputPin ? 1 : -1, 0);
			StateGridDimensions = BitCount switch
			{
				PinBitCount.Bit1 => new Vector2Int(1, 1),
				PinBitCount.Bit4 => new Vector2Int(2, 2),
				PinBitCount.Bit8 => new Vector2Int(4, 2),
				PinBitCount.Bit16 => new Vector2Int(4, 4),
				_ => throw new Exception("Bit count not implemented")
			};
			StateGridSize = BitCount switch
			{
				PinBitCount.Bit1 => Vector2.one * (DevPinStateDisplayRadius * 2 + DevPinStateDisplayOutline * 2),
				_ => (Vector2)StateGridDimensions * MultiBitPinStateDisplaySquareSize + Vector2.one * DevPinStateDisplayOutline
			};
		}

		public Vector2 HandlePosition => Position;
		public Vector2 StateDisplayPosition
		{
			get
			{
				Vector2 baseOffset = faceDir * (DevPinHandleWidth / 2 + StateGridSize.x / 2 + 0.065f);
				return HandlePosition + RotateVector(baseOffset, Rotation);
			}
		}

		public Vector2 PinPosition
		{
			get
			{
				int gridDst = BitCount is PinBitCount.Bit1 or PinBitCount.Bit4 ? 6 : 9;
				Vector2 baseOffset = faceDir * (GridSize * gridDst);
				return HandlePosition + RotateVector(baseOffset, Rotation);
			}
		}

		static Vector2 RotateVector(Vector2 v, int rotation)
		{
			// Rotate vector 90° clockwise per rotation step
			return rotation switch
			{
				0 => v,
				1 => new Vector2(v.y, -v.x),  // 90° clockwise
				2 => new Vector2(-v.x, -v.y), // 180°
				3 => new Vector2(-v.y, v.x),  // 270° clockwise
				_ => v
			};
		}


		public Vector2 Position { get; set; }
		public Vector2 MoveStartPosition { get; set; }
		public Vector2 StraightLineReferencePoint { get; set; }
		public int ID { get; }
		public int Rotation { get; set; } // 0, 1, 2, 3 representing 0°, 90°, 180°, 270° clockwise

		public bool IsSelected { get; set; }
		public bool HasReferencePointForStraightLineMovement { get; set; }
		public bool IsValidMovePos { get; set; }

		public Bounds2D SelectionBoundingBox => CreateBoundingBox(SelectionBoundsPadding);

		public Bounds2D BoundingBox => CreateBoundingBox(0);


		public Vector2 SnapPoint => Pin.GetWorldPos();

		public bool ShouldBeIncludedInSelectionBox(Vector2 selectionCentre, Vector2 selectionSize)
		{
			Bounds2D selfBounds = SelectionBoundingBox;
			return Maths.BoxesOverlap(selectionCentre, selectionSize, selfBounds.Centre, selfBounds.Size);
		}

		public int GetStateDecimalDisplayValue()
		{
			uint rawValue = PinState.GetBitStates(Pin.State);
			int displayValue = (int)rawValue;

			if (pinValueDisplayMode == PinValueDisplayMode.SignedDecimal)
			{
				displayValue = Maths.TwosComplement(rawValue, (int)BitCount);
			}

			return displayValue;
		}

		Bounds2D CreateBoundingBox(float pad)
		{
			Vector2 rotatedFaceDir = RotateVector(faceDir, Rotation);
			Vector2 handleSize = GetHandleSize();
			Vector2 pinPos = PinPosition;
			Vector2 pinOffset = RotateVector(faceDir * PinRadius, Rotation);
			
			Vector2 min = Vector2.Min(HandlePosition - rotatedFaceDir * handleSize.x / 2, pinPos - pinOffset);
			Vector2 max = Vector2.Max(HandlePosition + rotatedFaceDir * handleSize.x / 2, pinPos + pinOffset);
			
			Vector2 centre = (min + max) / 2;
			Vector2 size = max - min + Vector2.one * pad;
			return Bounds2D.CreateFromCentreAndSize(centre, size);
		}

		public Bounds2D HandleBounds() => Bounds2D.CreateFromCentreAndSize(HandlePosition, GetHandleSize());

		public float BoundsHeight() => StateGridSize.y;

		public Vector2 GetHandleSize()
		{
			Vector2 baseSize = new(DevPinHandleWidth, BoundsHeight());
			// Swap width/height when rotated 90° or 270°
			return (Rotation % 2 == 0) ? baseSize : new Vector2(baseSize.y, baseSize.x);
		}

		public void ToggleState(int bitIndex)
		{
			PinState.Toggle(ref Pin.PlayerInputState, bitIndex);
		}

		public bool PointIsInInteractionBounds(Vector2 point) => PointIsInHandleBounds(point) || PointIsInStateIndicatorBounds(point);

		public bool PointIsInStateIndicatorBounds(Vector2 point) => Maths.PointInCircle2D(point, StateDisplayPosition, DevPinStateDisplayRadius);

		public bool PointIsInHandleBounds(Vector2 point) => HandleBounds().PointInBounds(point);
	}
}