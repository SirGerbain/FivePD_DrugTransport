using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using FivePD.API;
using FivePD.API.Utils;

namespace SirGerbain_DrugTransport
{
    [CalloutProperties("Drug Transport", "SirGerbain", "1.0.0")]
    public class SirGerbain_DrugTransport : FivePD.API.Callout
    {
        Ped cartelLeader, cartelMember1, cartelMember2;
        List<VehicleHash> cartelVehicles = new List<VehicleHash>();
        Vector3 drugShipmentLocation;
        Random random = new Random();
        Vehicle drugShipmentVehicle;
        private float tickTimer = 0f;
        private float tickInterval = 1f;
        bool initiateChase = false;

        private string[] drugList = { "20 bags of Meth", "15 Joints", "Crack pipe with rocks", "Random Pills", "Heroin" };

        public SirGerbain_DrugTransport()
        {
            cartelVehicles.Add(VehicleHash.Baller);
            cartelVehicles.Add(VehicleHash.Cavalcade);
            cartelVehicles.Add(VehicleHash.Contender);

            float offsetX = random.Next(100, 200);
            float offsetY = random.Next(100, 200);
            Vector3 playerPos = Game.PlayerPed.Position;
            drugShipmentLocation = new Vector3(offsetX, offsetY, 0) + playerPos;
            drugShipmentLocation = World.GetNextPositionOnStreet(drugShipmentLocation);

            InitInfo(drugShipmentLocation + new Vector3(random.Next(1, 15), random.Next(1, 15), random.Next(1, 15)));
            ShortName = "Drug Transport";
            CalloutDescription = "Drug Transport";
            ResponseCode = 3;
            StartDistance = 200f;
        }

        public async override Task OnAccept()
        {
            InitBlip();
            UpdateData();

            PlayerData playerData = Utilities.GetPlayerData();
            string displayName = playerData.DisplayName;
            DrawSubtitle("~r~[911] ~y~Officer ~b~" + displayName + ",~y~ a dangerous drug cartel is transporting a shipment of illegal drugs through the city. We need you to track and intercept the shipment before it reaches its destination.", 7000);
        }

        public async override void OnStart(Ped closest) {

            base.OnStart(closest);

            await setupCallout();

            Tick += OnTick;
        }
        public async Task setupCallout()
        {

            drugShipmentVehicle = await SpawnVehicle(cartelVehicles[random.Next(0, cartelVehicles.Count)], drugShipmentLocation);
            drugShipmentVehicle.EnginePowerMultiplier = 2;
            VehicleData vehicleData = new VehicleData();
            vehicleData.Registration = false;
            vehicleData.Insurance = false;
            vehicleData.OwnerFirstName = "Adam";
            vehicleData.OwnerLastName = "Rudnick";
            Utilities.SetVehicleData(drugShipmentVehicle.NetworkId, vehicleData);
            Utilities.ExcludeVehicleFromTrafficStop(drugShipmentVehicle.NetworkId, true);

            cartelLeader = await SpawnPed(PedHash.BallaOrig01GMY, drugShipmentLocation);
            cartelLeader.Weapons.Give(WeaponHash.CombatPistol, 250, true, true);
            cartelMember1 = await SpawnPed(PedHash.BallaEast01GMY, drugShipmentLocation);
            cartelMember1.Weapons.Give(WeaponHash.Pistol, 250, true, true);
            cartelMember2 = await SpawnPed(PedHash.Ballas01GFY, drugShipmentLocation);
            cartelMember1.Weapons.Give(WeaponHash.SMG, 500, true, true);

            Vector3 coords = cartelLeader.Position;
            Vector3 closestVehicleNodeCoords;
            float roadheading;
            OutputArgument tempcoords = new OutputArgument();
            OutputArgument temproadheading = new OutputArgument();
            Function.Call<Vector3>(Hash.GET_CLOSEST_VEHICLE_NODE_WITH_HEADING, coords.X, coords.Y, coords.Z, tempcoords, temproadheading, 1, 3, 0);
            closestVehicleNodeCoords = tempcoords.GetResult<Vector3>();
            roadheading = temproadheading.GetResult<float>();
            drugShipmentVehicle.Heading = roadheading;

            cartelLeader.SetIntoVehicle(drugShipmentVehicle, VehicleSeat.Driver);
            cartelMember1.SetIntoVehicle(drugShipmentVehicle, VehicleSeat.RightFront);
            cartelMember2.SetIntoVehicle(drugShipmentVehicle, VehicleSeat.LeftRear);
            cartelLeader.AlwaysKeepTask = true;
            cartelLeader.BlockPermanentEvents = true;
            cartelLeader.Task.CruiseWithVehicle(drugShipmentVehicle, 25f, 525116);
            cartelMember1.AlwaysKeepTask = true;
            cartelMember1.BlockPermanentEvents = true;
            cartelMember2.AlwaysKeepTask = true;
            cartelMember2.BlockPermanentEvents = true;

        }

        public async Task OnTick()
        {
            tickTimer += Game.LastFrameTime;
            if (tickTimer >= tickInterval)
            {   
                if (initiateChase)
                {
                    API.SetDriveTaskMaxCruiseSpeed(cartelLeader.GetHashCode(), 250f);
                    API.SetDriveTaskDrivingStyle(cartelLeader.GetHashCode(), 524852);
                    cartelLeader.Task.FleeFrom(Game.PlayerPed);

                    if (random.Next(0,100)>45)
                    {
                        cartelMember1.Task.FightAgainst(Game.PlayerPed);
                        await BaseScript.Delay(random.Next(1500, 3900));
                        cartelMember1.Task.ClearAll();
                    }
                    if (random.Next(0, 100) > 45)
                    {
                        cartelMember2.Task.FightAgainst(Game.PlayerPed); 
                        await BaseScript.Delay(random.Next(1500, 3900));
                        cartelMember2.Task.ClearAll();
                    }
                }
                else
                {
                    InitBlip();
                    float distance = Game.PlayerPed.Position.DistanceTo(drugShipmentVehicle.Position);
                    if (distance < 50f)
                    {
                        initiateChase = true;
                        await BaseScript.Delay(5000);
                    }

                }

                tickTimer = 0f;
            }
            await BaseScript.Delay(0);
        }
        private void Notify(string message)
        {
            ShowNetworkedNotification(message, "CHAR_CALL911", "CHAR_CALL911", "Dispatch", "AIR-1", 15f);
        }
        private void DrawSubtitle(string message, int duration)
        {
            API.BeginTextCommandPrint("STRING");
            API.AddTextComponentSubstringPlayerName(message);
            API.EndTextCommandPrint(duration, false);
        }
    }
}

