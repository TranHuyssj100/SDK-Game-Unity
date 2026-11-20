// Assets/SDK/CurencyManager/Runtime/RegenWallet.cs
using System;

namespace LSDK.Currency
{
    /// <summary>
    /// Luật ví hồi theo thời gian, áp cho mọi currency bật regen trong catalog.
    /// Refresh cộng thẳng vào record. Chỉ setter đi qua <see cref="ApplyAmount"/>.
    /// </summary>
    public static class RegenWallet
    {
        /// <summary>
        /// Ghi số dư. Sàn 0. Unlimited còn hạn thì bỏ mọi lần giảm.
        /// Đang đầy và không có lịch thì lần tăng bị bỏ qua.
        /// Dưới trần, vừa rời trần hoặc chưa có lịch thì mở mốc <c>now + interval</c>.
        /// Đang hồi dở thì giữ mốc. Chạm trần thì xóa mốc.
        /// </summary>
        public static bool ApplyAmount(CurrencyRecord record, CurrencyDefinition def, long quantity, long now, out bool amountChanged)
        {
            amountChanged = false;
            if (record == null || def == null) return false;
            if (quantity < 0) quantity = 0;
            if (now < 0) now = 0;

            if (def.AllowUnlimited && UnlimitedRemaining(record, now) > 0 && quantity < record.Amount)
                return false;

            if (def.UseRegen
                && record.Amount >= def.MaxAmount
                && record.NextRegenUnix == 0
                && quantity >= record.Amount)
            {
                if (record.Seeded) return false;
                record.SetSeeded(true);
                return true;
            }

            int next = NextRegenUnix(record, def, quantity, now);
            bool amountDiff = quantity != record.Amount;
            bool timeDiff = next != record.NextRegenUnix;
            if (!amountDiff && !timeDiff && record.Seeded) return false;

            record.SetAmount(quantity);
            record.SetNextRegenUnix(next);
            record.SetSeeded(true);
            amountChanged = amountDiff;
            return true;
        }

        /// <summary>
        /// Bắt kịp đơn vị offline. Amount dưới trần mà chưa có lịch thì chỉ đặt lịch, chưa cộng.
        /// Mỗi interval đã trôi kể từ mốc cũ cộng một đơn vị. Chạm trần thì xóa lịch.
        /// </summary>
        public static bool Refresh(CurrencyRecord record, CurrencyDefinition def, long now, out bool amountChanged)
        {
            amountChanged = false;
            if (record == null || def == null) return false;
            if (now < 0) now = 0;

            bool flagChanged = SyncUnlimited(record, def, now);
            bool regenChanged = RefreshRegen(record, def, now, out amountChanged);
            return flagChanged || regenChanged;
        }

        /// <summary>
        /// Đồng bộ cờ <see cref="CurrencyRecord.IsUnlimited"/> theo hạn và cấu hình.
        /// Trả true nếu cờ đổi.
        /// </summary>
        public static bool SyncUnlimited(CurrencyRecord record, CurrencyDefinition def, long now)
        {
            if (record == null || def == null) return false;
            bool active = def.AllowUnlimited && record.UnlimitedUntilUnix > now;
            return record.SetUnlimited(active);
        }

        static bool RefreshRegen(CurrencyRecord record, CurrencyDefinition def, long now, out bool amountChanged)
        {
            amountChanged = false;

            if (!def.UseRegen)
                return ClearRegen(record);

            if (record.Amount >= def.MaxAmount)
                return ClearRegen(record);

            if (record.NextRegenUnix <= 0)
            {
                int scheduled = ClampUnix(AddInterval(now, def.RegenIntervalSeconds));
                if (record.NextRegenUnix == scheduled) return false;
                record.SetNextRegenUnix(scheduled);
                return true;
            }

            if (now < record.NextRegenUnix) return false;

            int interval = def.RegenIntervalSeconds;
            long marker = record.NextRegenUnix;
            long amount = record.Amount;
            long room = def.MaxAmount - amount;
            long elapsed = now - marker;
            if (elapsed < 0) elapsed = 0;

            long steps = elapsed / interval;
            if (steps < long.MaxValue) steps += 1;
            if (steps > room) steps = room;
            if (steps <= 0) return false;

            amount += steps;
            bool reachedCap = amount >= def.MaxAmount;
            int next = reachedCap ? 0 : ClampUnix(Advance(marker, steps, interval));
            amountChanged = amount != record.Amount;
            bool timeChanged = next != record.NextRegenUnix;
            if (!amountChanged && !timeChanged) return false;

            record.SetAmount(amount);
            record.SetNextRegenUnix(next);
            return true;
        }

        /// <summary>Giây còn lại tới đơn vị kế tiếp. 0 khi đầy, chưa có lịch, đã tới mốc, hoặc không hồi.</summary>
        public static int RegenRemaining(CurrencyRecord record, CurrencyDefinition def, long now)
        {
            if (record == null || def == null || !def.UseRegen) return 0;
            if (record.Amount >= def.MaxAmount) return 0;
            if (record.NextRegenUnix <= 0) return 0;
            if (now >= record.NextRegenUnix) return 0;
            long diff = record.NextRegenUnix - now;
            if (diff > int.MaxValue) return int.MaxValue;
            return (int)diff;
        }

        /// <summary>
        /// Cộng dồn unlimited từ <c>max(now, until)</c>.
        /// Amount dưới trần thì đi qua setter để đầy trần và xóa lịch hồi.
        /// </summary>
        public static bool AddUnlimited(CurrencyRecord record, CurrencyDefinition def, long seconds, long now, out bool amountChanged)
        {
            amountChanged = false;
            if (record == null || def == null || !def.AllowUnlimited || seconds <= 0) return false;
            if (now < 0) now = 0;

            long start = record.UnlimitedUntilUnix > now ? record.UnlimitedUntilUnix : now;
            long until = seconds > long.MaxValue - start ? long.MaxValue : start + seconds;
            bool changed = until != record.UnlimitedUntilUnix;
            record.SetUnlimitedUntilUnix(until);
            if (SyncUnlimited(record, def, now)) changed = true;

            if (def.UseRegen && record.Amount < def.MaxAmount)
            {
                bool amountDidChange;
                if (ApplyAmount(record, def, def.MaxAmount, now, out amountDidChange))
                    changed = true;
                if (amountDidChange) amountChanged = true;
            }

            return changed;
        }

        /// <summary>Giây unlimited còn lại. 0 khi đã hết hạn.</summary>
        public static long UnlimitedRemaining(CurrencyRecord record, long now)
        {
            if (record == null || record.UnlimitedUntilUnix <= now) return 0;
            return record.UnlimitedUntilUnix - now;
        }

        /// <summary>Chưa seed thì gán đầy trần nếu có hồi, ngược lại gán số khởi tạo. Không đụng unlimited.</summary>
        public static bool Seed(CurrencyRecord record, CurrencyDefinition def)
        {
            if (record == null || def == null || record.Seeded) return false;
            long amount = def.UseRegen ? def.MaxAmount : def.InitialAmount;
            if (amount < 0) amount = 0;
            record.SetAmount(amount);
            record.SetNextRegenUnix(0);
            record.SetSeeded(true);
            return true;
        }

        /// <summary>Kẹp Unix giây về <c>0 … int.MaxValue</c>.</summary>
        public static int ClampUnix(long value)
        {
            if (value <= 0) return 0;
            if (value >= int.MaxValue) return int.MaxValue;
            return (int)value;
        }

        static int NextRegenUnix(CurrencyRecord record, CurrencyDefinition def, long quantity, long now)
        {
            if (!def.UseRegen) return 0;
            if (quantity >= def.MaxAmount) return 0;
            if (record.Amount >= def.MaxAmount || record.NextRegenUnix <= 0)
                return ClampUnix(AddInterval(now, def.RegenIntervalSeconds));
            return record.NextRegenUnix;
        }

        static bool ClearRegen(CurrencyRecord record)
        {
            if (record.NextRegenUnix == 0) return false;
            record.SetNextRegenUnix(0);
            return true;
        }

        static long AddInterval(long now, int interval)
        {
            if (interval < 1) interval = 1;
            if (now > long.MaxValue - interval) return long.MaxValue;
            return now + interval;
        }

        static long Advance(long marker, long steps, int interval)
        {
            if (steps <= 0) return marker;
            if (interval < 1) interval = 1;
            if (steps > long.MaxValue / interval) return long.MaxValue;
            long delta = steps * interval;
            if (marker > long.MaxValue - delta) return long.MaxValue;
            return marker + delta;
        }
    }
}
