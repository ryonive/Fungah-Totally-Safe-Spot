using Dalamud.Game.ClientState;
using Dalamud.Game.Gui;
using Dalamud.Interface;
using Dalamud.Bindings.ImGui;
using System;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Plugin.Services;
using Dalamud.Interface.Utility;
using Fungah;
using Dalamud.Game.ClientState.Objects.SubKinds;

namespace SamplePlugin
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    public unsafe class PluginUI : IDisposable
    {
        private readonly IClientState _clientState;
        private readonly IGameGui _gameGui;
        private readonly IObjectTable _objectTable;

        // south of stage : <70.78049, -4.472919, -21.072674>
        // east of stage  : <85.45761, -4.4729047, -36.12376>
        // north of stage : <70.41719, -4.4729795, -50.762985>
        // west of stage  : <55.68961, -4.4729276, -35.709606>
        // 'Safe' spot    : <66.96, -4.48, -24.69> 

        // Supercilious Spellweaver data id - https://github.com/xivapi/ffxiv-datamining/blob/master/csv/ENpcResident.csv
        private const uint FungahNpcId = 1010476;
        private const float StageNorth = -50.76f;
        private const float StageSouth = -21f;
        private const float StageEast = 85.45f;
        private const float StageWest = 55.6f;
        private readonly Vector3 _safeSpot = new Vector3(66.96f, -4.48f, -24.69f);
        private readonly uint _goldSaucerMapID = 144;
        private readonly float _circleRadius = 5f;
        private readonly uint _red = ImGui.GetColorU32(ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0, 0, 1f)));
        private readonly uint _green = ImGui.GetColorU32(ImGui.ColorConvertFloat4ToU32(new Vector4(0, 1, 0, 1f)));


        public PluginUI(IClientState clientState, IGameGui gameGui, IObjectTable objectTable)
        {
            _clientState = clientState;
            _gameGui = gameGui;
            _objectTable = objectTable;
        }


        public void Dispose()
        {
        }

        public void Draw()
        {
            if (PlayerOnStage()) DrawCircleWindow();
        }

        /// <summary>
        /// Draws the safe spot
        /// </summary>
        private void DrawCircleWindow()
        {
            Vector2 circlePos;
            _gameGui.WorldToScreen(_safeSpot, out circlePos);
            var winPos = new Vector2(circlePos.X - 15, circlePos.Y - 15);

#if DEBUG
            PluginLog.Debug($"Drawing Circle At: {circlePos}");
            PluginLog.Debug($"Drawing Window At: {winPos}");
#endif

            ImGuiHelpers.SetNextWindowPosRelativeMainViewport(winPos);
            ImGui.SetNextWindowSize(new Vector2(90, 50) * ImGuiHelpers.GlobalScale);
            if (ImGui.Begin("Pointer",  ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoInputs))
            {
                DrawCircle(circlePos);
                DrawCalibrationArrow();
                ImGui.End();
            }
        }

        /// <summary>
        /// Returns true if player position is on the safe spot
        /// </summary>
        /// <returns></returns>
        private bool PlayerAtSafeSpot()
        {   //shouldn't be null if this is called...
            var player = GetLocalPlayer();
            return player != null &&
                Vector3.DistanceSquared(player.Position, _safeSpot) < 0.00025; //distance from safe spot
        }

        /// <summary>
        /// Returns true if player is near the safe spot
        /// </summary>
        /// <returns></returns>
        private bool PlayerNearSafeSpot()
        {
            var player = GetLocalPlayer();
            return player != null &&
                   Vector3.DistanceSquared(player.Position, _safeSpot) < 0.05; //distance from safe spot
        }

        /// <summary>
        /// Draws a red circle, turns green when player is really close.
        /// </summary>
        /// <param name="pos"></param>
        private void DrawCircle(Vector2 pos)
        {
#if DEBUG
            PluginLog.Debug($"int: Drawing Circle At: {pos}");
#endif
            if (PlayerAtSafeSpot()) ImGui.GetWindowDrawList().AddCircleFilled(pos, _circleRadius, _green);
            else ImGui.GetWindowDrawList().AddCircleFilled(pos, _circleRadius, _red);
        }

        /// <summary>
        /// Checks what the name says
        /// </summary>
        /// <returns></returns>
        private bool PlayerAtGoldSaucer() => _clientState.TerritoryType == _goldSaucerMapID;

        /// <summary>
        /// Checks if the player is on the stage during the fungah GATE, based on the stages NESW positions
        /// </summary>
        /// <returns></returns>
        private bool PlayerOnStage()
        {
            var player = GetLocalPlayer();
            if (player == null || !PlayerAtGoldSaucer() || !IsFungahEvent()) return false;            
            var pos = player.Position;
            // south and north are negative values.
            return ((pos.X is > StageWest and < StageEast) && (pos.Z is < StageSouth and > StageNorth));
        }

        /// <summary>
        /// Checks if fungah GATE is running by looking for the event NPC dataId of "Supercilious Spellweaver"
        /// </summary>
        /// <returns></returns>
        private bool IsFungahEvent()
        {
#if DEBUG
            PluginLog.Debug($"npc: {_objectTable.Any(o => o.BaseId == FungahNpcId)}");
            return true;
#else
            return _objectTable.Any(o => o.DataId == FungahNpcId);
#endif
        }

        /// <summary>
        /// Draws movement helper text when near, but not on, the safe spot
        /// </summary>
        private void DrawCalibrationArrow()
        {
            if (PlayerAtSafeSpot() || !PlayerNearSafeSpot()) return;
            var pos = GetLocalPlayer()!.Position;
            // gud enuf

            ImGui.SetCursorPosY(24f);            
            ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0,0,0,0.8f));           
            ImGui.BeginChild("dfsaf", new Vector2(80f, 20f) * ImGuiHelpers.GlobalScale);
            ImGui.SetCursorPosX(4f * ImGuiHelpers.GlobalScale);

            if (pos.X - _safeSpot.X > 0.015) ImGui.Text("move left");
            else if (_safeSpot.X - pos.X > 0.015) ImGui.Text("move right");
            else if (pos.Z < _safeSpot.Z) ImGui.Text("move down");
            else if(pos.Z > _safeSpot.Z) ImGui.Text("move up");
            
            ImGui.EndChild();
            ImGui.PopStyleColor();
            
        }

        private IPlayerCharacter? GetLocalPlayer() => _objectTable.LocalPlayer;
    }
}

