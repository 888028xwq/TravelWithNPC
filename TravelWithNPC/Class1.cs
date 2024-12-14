using System;
using System.Collections.Generic;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;

namespace TravelWithNPC
{
    public class RandomNpcSpawner : Script
    {
        private List<Ped> npcList = new List<Ped>();
        private List<Blip> npcBlips = new List<Blip>();  // 儲存NPC的地圖標記
        private bool showInteractionMenu = false;
        private bool isTraveling = false;
        private bool isNearNpc = false;
        private bool destinationReached = false;
        private Ped targetNpc;
        private Random random = new Random();
        private Vector3 destination;
        private int spawnInterval = 30000;
        private DateTime lastSpawnTime = DateTime.Now;
        private float interactionDistance = 3.0f;
        private float destinationDistanceThreshold = 10.0f;

        public RandomNpcSpawner()
        {
            Tick += OnTick;
            KeyDown += OnKeyDown;

        }

        private void OnTick(object sender, EventArgs e)
        {
            if (isTraveling)
            {
                // 檢查是否達到目的地
                CheckDestinationReached();
            }
            else
            {
                // 如果沒有進行旅行活動，則隨機生成NPC
                if ((DateTime.Now - lastSpawnTime).TotalMilliseconds > spawnInterval)
                {
                    SpawnRandomNpcs();
                    lastSpawnTime = DateTime.Now;
                }

                // 檢查玩家與NPC的距離


                // 檢查玩家與NPC的距離
                CheckPlayerProximity();
            }
        }

        private void SpawnRandomNpcs()
        {
            // 檢查現有的 NPC 清單，如果有存活的 NPC，則不生成新的 NPC
            if (npcList.Count > 0 && npcList.Exists(npc => npc != null && npc.IsAlive))
            {
                return;
            }


            // 清除現有的 NPC 和地圖標記
            ClearNpcs();

            Vector3 spawnPosition = new Vector3(1262.971f, 2668.598f, 0.0f);  // 設定 NPC 固定生成的座標

            // 生成 NPC 和地圖標記
            for (int i = 0; i < random.Next(1,1); i++)
            {
                //Vector3 spawnPosition = World.GetNextPositionOnStreet(Game.Player.Character.Position + new Vector3(random.Next(-100, 100), random.Next(-100, 100), 0));
                Ped npc = World.CreateRandomPed(spawnPosition);
                npcList.Add(npc);

                Vehicle vehicle = World.CreateVehicle(VehicleHash.Bati, spawnPosition);
                npc.SetIntoVehicle(vehicle, VehicleSeat.Driver);

                // 創建NPC的地圖標記
                Blip blip = npc.AddBlip();
                blip.Color = BlipColor.Blue;
                blip.Scale = 0.8f;
                npcBlips.Add(blip);
            }
        }

        private void CheckPlayerProximity()
        {
            isNearNpc = false;
            foreach (Ped npc in npcList)
            {
                if (npc != null && npc.IsAlive && Game.Player.Character.Position.DistanceTo(npc.Position) < interactionDistance)
                {
                    isNearNpc = true;
                    targetNpc = npc;
                    UI.ShowSubtitle("Press E to interact with NPC");
                    break;
                }
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.E && isNearNpc && !isTraveling)
            {
                ShowInteractionMenu();
                showInteractionMenu = true; // 顯示互動選單
            }
            else if (showInteractionMenu)
            {
                HandleInteractionMenu(e);
            }
            else if (e.KeyCode == Keys.Y && destinationReached)
            {
                UI.Notify("Set your next GPS destination!");
                isTraveling = true;
                destinationReached = false;
            }
            else if (e.KeyCode == Keys.N && destinationReached)
            {
                StopTravelMode();
            }
        }

        private void ShowInteractionMenu()
        {
            UI.Notify("Do you want to start a trip with this NPC? Press Y for Yes or N for No");
        }

        private void HandleInteractionMenu(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Y)
            {
                StartTravelMode();
                showInteractionMenu = false;  // 關閉互動選單
            }
            else if (e.KeyCode == Keys.N)
            {
                UI.Notify("Trip canceled.");
                ClearNpcs();
                showInteractionMenu = false;  // 關閉互動選單
            }
        }

        private void StartTravelMode()
        {
            isTraveling = true;

            foreach (Ped npc in npcList)
            {
                if (npc.IsInVehicle())
                {
                    //Vehicle npcVehicle = npc.CurrentVehicle;

                    // 設置 NPC 車輛的跟隨行為
                    npc.Task.DriveTo(
                        npc.CurrentVehicle,                            // NPC 的車輛
                        Game.Player.Character.Position + new Vector3(100, 0, 0),        // 玩家位置（目標）
                        5.0f,                   // 與玩家保持的最小距離
                        10.0f,                          // 駕駛速度
                        30  // 駕駛風格，跟隨交通法則
                    );
                }
            }

            UI.Notify("The NPCs are ready. Set your GPS destination and start the journey!");
        }

        private void CheckDestinationReached()
        {
            if (destination != Vector3.Zero && Game.Player.Character.Position.DistanceTo(destination) < destinationDistanceThreshold)
            {
                destinationReached = true;
                ShowArrivalMenu();
            }

            // 停止所有 NPC 的跟隨任務
            foreach (Ped npc in npcList)
            {
                npc.Task.ClearAll(); // 清除 NPC 任務，停止跟隨
            }
        }

        private void ShowArrivalMenu()
        {
            isTraveling = false;
            destination = Vector3.Zero;
            UI.ShowSubtitle("You've arrived at your destination. Continue trip? Press Y for Yes or N for No.");
        }

        private void StopTravelMode()
        {
            isTraveling = false;
            ClearNpcs();
            UI.Notify("Trip ended. Thanks for traveling with us!");

            // 重新生成 NPC
            SpawnRandomNpcs();
        }

        private void ClearNpcs()
        {
            // 清除所有 NPC 和其對應的地圖標記
            foreach (var npc in npcList)
            {
                npc?.Delete();
            }
            npcList.Clear();

            foreach (var blip in npcBlips)
            {
                blip?.Remove();
            }
            npcBlips.Clear();
        }
    }
}
