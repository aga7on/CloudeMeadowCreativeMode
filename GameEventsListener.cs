using System;
using System.Reflection;
using TeamNimbus.CloudMeadow.Managers;
using UnityEngine;

namespace CloudMeadow.CreativeMode
{
    internal class GameEventsListener : MonoBehaviour
    {
        private Delegate _seasonChangedHandler;
        private Delegate _startOfNewDayHandler;
        private Delegate _hourChangedHandler;

        private void OnEnable()
        {
            TryRegisterEvents();
        }

        private void OnDisable()
        {
            TryUnregisterEvents();
        }

        private void TryRegisterEvents()
        {
            try
            {
                var gmType = typeof(GameManager);

                // SeasonChangedEvent (no args)
                var seasonField = gmType.GetField("SeasonChangedEvent", BindingFlags.Public | BindingFlags.Static);
                var seasonEvt = (seasonField != null) ? seasonField.GetValue(null) : null;
                if (seasonEvt != null)
                {
                    var mi = seasonEvt.GetType().GetMethod("RegisterHandler", new[] { typeof(Func<bool>) });
                    if (mi == null) mi = seasonEvt.GetType().GetMethod("RegisterHandler", new[] { typeof(Action) });
                    if (mi != null)
                    {
                        Action a = () => LogBuffer.Add("Season changed");
                        _seasonChangedHandler = a;
                        mi.Invoke(seasonEvt, new object[] { a });
                    }
                }

                // StartOfNewDayEvent (no args)
                var newDayField = gmType.GetField("StartOfNewDayEvent", BindingFlags.Public | BindingFlags.Static);
                var newDayEvt = (newDayField != null) ? newDayField.GetValue(null) : null;
                if (newDayEvt != null)
                {
                    var mi = newDayEvt.GetType().GetMethod("RegisterHandler", new[] { typeof(Func<bool>) });
                    if (mi == null) mi = newDayEvt.GetType().GetMethod("RegisterHandler", new[] { typeof(Action) });
                    if (mi != null)
                    {
                        Action a = () => LogBuffer.Add("Start of new day");
                        _startOfNewDayHandler = a;
                        mi.Invoke(newDayEvt, new object[] { a });
                    }
                }

                // HourChangedEvent (int hours)
                var hourField = gmType.GetField("HourChangedEvent", BindingFlags.Public | BindingFlags.Static);
                var hourEvt = (hourField != null) ? hourField.GetValue(null) : null;
                if (hourEvt != null)
                {
                    var mi = hourEvt.GetType().GetMethod("RegisterHandler", new[] { typeof(Func<int, bool>) });
                    if (mi == null) mi = hourEvt.GetType().GetMethod("RegisterHandler", new[] { typeof(Action<int>) });
                    if (mi != null)
                    {
                        Action<int> a = (h) => LogBuffer.Add("Hours passed: " + h);
                        _hourChangedHandler = a;
                        mi.Invoke(hourEvt, new object[] { a });
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("GameEventsListener register failed: " + e.Message);
            }
        }

        private void TryUnregisterEvents()
        {
            try
            {
                var gmType = typeof(GameManager);

                var seasonField = gmType.GetField("SeasonChangedEvent", BindingFlags.Public | BindingFlags.Static);
                var seasonEvt = (seasonField != null) ? seasonField.GetValue(null) : null;

                var newDayField = gmType.GetField("StartOfNewDayEvent", BindingFlags.Public | BindingFlags.Static);
                var newDayEvt = (newDayField != null) ? newDayField.GetValue(null) : null;

                var hourField = gmType.GetField("HourChangedEvent", BindingFlags.Public | BindingFlags.Static);
                var hourEvt = (hourField != null) ? hourField.GetValue(null) : null;

                if (seasonEvt != null && _seasonChangedHandler != null)
                {
                    var unregS = seasonEvt.GetType().GetMethod("UnregisterHandler");
                    if (unregS != null) unregS.Invoke(seasonEvt, new object[] { _seasonChangedHandler });
                }
                if (newDayEvt != null && _startOfNewDayHandler != null)
                {
                    var unregD = newDayEvt.GetType().GetMethod("UnregisterHandler");
                    if (unregD != null) unregD.Invoke(newDayEvt, new object[] { _startOfNewDayHandler });
                }
                if (hourEvt != null && _hourChangedHandler != null)
                {
                    var unregH = hourEvt.GetType().GetMethod("UnregisterHandler");
                    if (unregH != null) unregH.Invoke(hourEvt, new object[] { _hourChangedHandler });
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("GameEventsListener unregister failed: " + e.Message);
            }
        }
    }
}
