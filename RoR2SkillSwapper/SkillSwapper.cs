using BepInEx;
using RoR2;
using RoR2.Skills;
using RoR2.UI;
using System.IO;
using System.Linq;
using UnityEngine.Networking;
using static RoR2SkillSwapper.Utils;

namespace RoR2SkillSwapper
{
    [BepInPlugin("twoface.skillswapper", "Skill Swapper", "2.1.1")]
    public class SkillSwapper : BaseUnityPlugin
    {
        private int?[] _skillReplacementIndices;
        private SkillDef[] _skillReplacements;
        private int _skillDefCount;

        public void Awake()
        {
            On.RoR2.UI.ChatBox.SubmitChat += ChatHook;

            On.RoR2.CharacterMaster.OnBodyStart += (orig, self, body) =>
            {
                orig.Invoke(self, body);

                if (self.playerCharacterMasterController && 
                    self.playerCharacterMasterController.networkUserObject &&
                    self.playerCharacterMasterController.networkUserObject.GetComponent<NetworkIdentity>().isLocalPlayer)
                {
                    Debug("Spawned local body; reapplying");
                    Reapply();
                }
            };
               
            _skillReplacementIndices = new int?[4];
            _skillReplacements = new SkillDef[4];
        }

        private void Init()
        {
            if (_skillDefCount == 0)
                _skillDefCount = SkillCatalog.allSkillDefs.Count();
        }

        private void ChatHook(On.RoR2.UI.ChatBox.orig_SubmitChat orig, ChatBox chatbox)
        {
            var field = chatbox.inputField;
            var text = field.text;
            var args = text.Split(' ');

            if (text.StartsWith("/"))
            {
                text = text.Substring(1);

                if (text.StartsWith("dump"))
                {
                    Dump();
                    field.text = "";
                }
                else if (text.StartsWith("swapi"))
                {
                    if (args.Length > 2)
                    {
                        Swap(args[1], args[2]);
                    }
                    else
                    {
                        Chat.AddMessage("usage: /swapi <slot> <skill_index>");
                    }
                    field.text = "";
                }
                else if (text.StartsWith("swap"))
                {
                    if (args.Length > 2)
                    {
                        SwapS(args[1], args[2]);
                    }
                    else
                    {
                        Chat.AddMessage("usage: /swap <slot> <skill_name>");
                    }
                    field.text = "";
                }
                else if (text.StartsWith("reapply"))
                {
                    Reapply();
                    field.text = "";
                }
                else if (text.StartsWith("reset"))
                {
                    if (args.Length > 1)
                    {
                        Reset(args[1]);
                    }
                    else
                    {
                        Chat.AddMessage("usage: /reset <slot>");
                    }
                    field.text = "";
                }
            }

            orig.Invoke(chatbox);
        }

        private void Log(string s) => Logger.LogInfo(s);
        private void Debug(string s) => Logger.LogDebug(s);        

        private void Dump()
        {
            using (var writer = new StreamWriter("dump.txt"))
            {
                writer.WriteLine("Skill indices\nindex, skill name token, skill name (inspector)\n");
                var count = SkillCatalog.allSkillDefs.Count();
                for (var i = 0; i < count; i++)
                {
                    var skill = SkillCatalog.GetSkillDef(i);

                    writer.WriteLine($"{i}, {skill.skillNameToken}, {skill.skillName}");
                }

                writer.Flush();
            }
        }

        private void Swap(string slot, string ind)
        {
            Init();
            if (!int.TryParse(ind, out var index))
            {
                Chat.AddMessage("Invalid index");
                return;
            }

            int slotNum = StringToSlot(slot);

            if (slotNum == -1 || slotNum > 3)
            {
                Chat.AddMessage("Invalid slot");
                return;
            }

            if (index < 0 || index > _skillDefCount)
            {
                Chat.AddMessage("Invalid skill");
                return;
            }

            SetSkill(slotNum, index);
        }

        private void SwapS(string slot, string name)
        {
            Init();
            var index = SkillCatalog.FindSkillIndexByName(name);
            int slotNum = StringToSlot(slot);

            if (slotNum == -1 || slotNum > 3)
            {
                Chat.AddMessage("Invalid slot");
                return;
            }

            if (index < 0 || index > _skillDefCount)
            {
                Chat.AddMessage($"Couldn't find skill: {name}");
                return;
            }

            SetSkill(slotNum, index);
        }

        private void Reset(string slot)
        {
            int slotNum = StringToSlot(slot);

            if (slotNum == -1 || slotNum > 3)
            {
                Chat.AddMessage("Invalid slot");
                return;
            }

            Remove(this, GetSlot(slotNum), _skillReplacements[slotNum]);
            _skillReplacementIndices[slotNum] = null;
            _skillReplacements[slotNum] = null;
            Chat.AddMessage($"Reset slot: {slot}");
        }

        private void SetSkill(int slot, int index)
        {
            var def = SkillCatalog.GetSkillDef(index);

            if (def == null)
            {
                Chat.AddMessage($"Couldn't find a skill at index: {index}");
                return;
            }
            var skillSlot = GetSlot(slot);

            if (_skillReplacements[slot] != null)
                Remove(this, skillSlot, _skillReplacements[slot]);

            Override(this, skillSlot, def);
            _skillReplacementIndices[slot] = index;
            _skillReplacements[slot] = def;

            Chat.AddMessage($"Swapped skill to \"{def.skillName}\"");
        }

        private void Reapply()
        {
            for (var i = 0; i < 4; i++)
            {
                if (_skillReplacements[i] != null)
                {
                    Override(this, GetSlot(i), _skillReplacements[i]);
                }
            }
        }
    }
}
