using System;
using System.Collections.Generic;
using System.Linq;

public static class EnumExtensions
{
    /// <summary>
    /// Checks wheter or not this enums is valid.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    private static void CheckEnumWithFlags<T>()
    {
        if (!typeof(T).IsEnum)
            throw new ArgumentException(string.Format("Type '{0}' is not an enum", typeof(T).FullName));
        if (!Attribute.IsDefined(typeof(T), typeof(FlagsAttribute)))
            throw new ArgumentException(string.Format("Type '{0}' doesn't have the 'Flags' attribute", typeof(T).FullName));
    }

    /// <summary>
    /// Checks if the Enums contains given flag.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <param name="flag"></param>
    /// <returns></returns>
    public static bool HasFlag<T>(this T value, T flag) where T : struct
    {
        CheckEnumWithFlags<T>();
        long lValue = Convert.ToInt64(value);
        long lFlag = Convert.ToInt64(flag);
        return (lValue & lFlag) != 0;
    }

    /// <summary>
    /// Gets flags contained by Enum as IEnumerable.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    public static IEnumerable<T> GetFlags<T>(this T value) where T : struct
    {
        CheckEnumWithFlags<T>();
        foreach (T flag in Enum.GetValues(typeof(T)).Cast<T>())
        {
            if (value.HasFlag(flag))
                yield return flag;
        }
    }

    /// <summary>
    /// Gets flags contained by Enum as Array.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    public static T[] GetFlagsAsArray<T>(this T value) where T : struct
    {
        CheckEnumWithFlags<T>();
        return Enum.GetValues(typeof(T)).Cast<T>().Where(x => value.HasFlag(x)).ToArray();
    }

    /// <summary>
    /// Gets number of flags contained by Enum.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    public static int GetNumberOfFlagsSet<T>(this T value) where T : struct
    {
        CheckEnumWithFlags<T>();
        return Enum.GetValues(typeof(T)).Cast<T>().Count(x => value.HasFlag(x));
    }

    /// <summary>
    /// Adds or Removes given flag from the Enum depending on "on" parameter..
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <param name="flags"></param>
    /// <param name="on">True: Add, False: Remove</param>
    /// <returns></returns>
    public static T SetFlags<T>(this T value, T flags, bool on) where T : struct
    {
        CheckEnumWithFlags<T>();
        long lValue = Convert.ToInt64(value);
        long lFlag = Convert.ToInt64(flags);
        if (on)
        {
            lValue |= lFlag;
        }
        else
        {
            lValue &= (~lFlag);
        }
        return (T)Enum.ToObject(typeof(T), lValue);
    }

    /// <summary>
    /// Adds given flag to the Enum.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <param name="flags"></param>
    /// <param name="on"></param>
    /// <returns></returns>
    public static T SetFlags<T>(this T value, T flags) where T : struct
    {
        return value.SetFlags(flags, true);
    }

    /// <summary>
    /// Removes given flags from the enum.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <param name="flags"></param>
    /// <returns></returns>
    public static T ClearFlags<T>(this T value, T flags) where T : struct
    {
        return value.SetFlags(flags, false);
    }

    /// <summary>
    /// Combines flags.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="flags"></param>
    /// <returns></returns>
    public static T CombineFlags<T>(this IEnumerable<T> flags) where T : struct
    {
        CheckEnumWithFlags<T>();
        long lValue = 0;
        foreach (T flag in flags)
        {
            long lFlag = Convert.ToInt64(flag);
            lValue |= lFlag;
        }
        return (T)Enum.ToObject(typeof(T), lValue);
    }

    /// <summary>
    /// Get random flag from the Enum.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    public static T GetRandomValue<T>(this T value) where T : struct
    {
        CheckEnumWithFlags<T>();
        Random random = new Random();
        T[] flagValues = Enum.GetValues(typeof(T)).Cast<T>().Where(x => value.HasFlag(x)).ToArray();
        return flagValues[random.Next(0, flagValues.Length)];
    }
}