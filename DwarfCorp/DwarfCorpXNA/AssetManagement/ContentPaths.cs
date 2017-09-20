// ContentPaths.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System.Collections.Generic;
using System.IO;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{

    /// <summary>
    /// This class is auto-generated. It exists to allow intellisense and compile-time awareness
    /// for content. This is to prevent inlining of file paths and mis-spellings.
    /// </summary>
    public class ContentPaths
    {
        public static string controls = ProgramData.CreatePath(DwarfGame.GetGameDirectory(), "controls.json");
        public static string settings = ProgramData.CreatePath(DwarfGame.GetGameDirectory(), "settings.json");
        public static string mixer = ProgramData.CreatePath(DwarfGame.GetGameDirectory(), "mixer.json");
    public class Audio
    {
        public class Oscar
        {
            public static string sfx_amb_grassland_day_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_amb_grassland_day_1");
            public static string sfx_amb_grassland_day_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_amb_grassland_day_2");
            public static string sfx_amb_grassland_night_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_amb_grassland_night_1");
            public static string sfx_amb_grassland_night_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_amb_grassland_night_2");
            public static string sfx_env_bush_harvest_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_env_bush_harvest_1");
            public static string sfx_env_bush_harvest_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_env_bush_harvest_2");
            public static string sfx_env_bush_harvest_3 = ProgramData.CreatePath("Audio", "oscar", "sfx_env_bush_harvest_3");
            public static string sfx_env_door_close_generic = ProgramData.CreatePath("Audio", "oscar", "sfx_env_door_close_generic");
            public static string sfx_env_door_open_generic = ProgramData.CreatePath("Audio", "oscar", "sfx_env_door_open_generic");
            public static string sfx_env_tree_cut_down_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_env_tree_cut_down_1");
            public static string sfx_env_tree_cut_down_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_env_tree_cut_down_2");
            public static string sfx_env_voxel_dirt_destroy = ProgramData.CreatePath("Audio", "oscar", "sfx_env_voxel_dirt_destroy");
            public static string sfx_env_voxel_metal_destroy = ProgramData.CreatePath("Audio", "oscar", "sfx_env_voxel_metal_destroy");
            public static string sfx_env_voxel_sand_destroy = ProgramData.CreatePath("Audio", "oscar", "sfx_env_voxel_sand_destroy");
            public static string sfx_env_voxel_snow_destroy = ProgramData.CreatePath("Audio", "oscar", "sfx_env_voxel_snow_destroy");
            public static string sfx_env_voxel_stone_destroy = ProgramData.CreatePath("Audio", "oscar", "sfx_env_voxel_stone_destroy");
            public static string sfx_env_voxel_wood_destroy = ProgramData.CreatePath("Audio", "oscar", "sfx_env_voxel_wood_destroy");
            public static string sfx_env_water_splash = ProgramData.CreatePath("Audio", "oscar", "sfx_env_water_splash");
            public static string sfx_gui_change_selection = ProgramData.CreatePath("Audio", "oscar", "sfx_gui_change_selection");
            public static string sfx_gui_click_voxel = ProgramData.CreatePath("Audio", "oscar", "sfx_gui_click_voxel");
            public static string sfx_gui_confirm_selection = ProgramData.CreatePath("Audio", "oscar", "sfx_gui_confirm_selection");
            public static string sfx_gui_daytime = ProgramData.CreatePath("Audio", "oscar", "sfx_gui_daytime");
            public static string sfx_gui_negative_generic = ProgramData.CreatePath("Audio", "oscar", "sfx_gui_negative_generic");
            public static string sfx_gui_nighttime = ProgramData.CreatePath("Audio", "oscar", "sfx_gui_nighttime");
            public static string sfx_gui_positive_generic = ProgramData.CreatePath("Audio", "oscar", "sfx_gui_positive_generic");
            public static string sfx_gui_positive_great_success = ProgramData.CreatePath("Audio", "oscar", "sfx_gui_positive_great_success");
            public static string sfx_gui_rain_storm_alert = ProgramData.CreatePath("Audio", "oscar", "sfx_gui_rain_storm_alert");
            public static string sfx_gui_snow_storm_alert = ProgramData.CreatePath("Audio", "oscar", "sfx_gui_snow_storm_alert");
            public static string sfx_gui_window_close = ProgramData.CreatePath("Audio", "oscar", "sfx_gui_window_close");
            public static string sfx_gui_window_open = ProgramData.CreatePath("Audio", "oscar", "sfx_gui_window_open");
            public static string sfx_ic_demon_angered = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_demon_angered");
            public static string sfx_ic_demon_mumble_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_demon_mumble_1");
            public static string sfx_ic_demon_mumble_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_demon_mumble_2");
            public static string sfx_ic_demon_mumble_3 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_demon_mumble_3");
            public static string sfx_ic_demon_mumble_4 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_demon_mumble_4");
            public static string sfx_ic_demon_mumble_5 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_demon_mumble_5");
            public static string sfx_ic_demon_mumble_6 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_demon_mumble_6");
            public static string sfx_ic_demon_pleased = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_demon_pleased");
            public static string sfx_ic_dwarf_angered = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_angered");
            public static string sfx_ic_dwarf_attack_musket_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_attack_musket_1");
            public static string sfx_ic_dwarf_attack_musket_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_attack_musket_2");
            public static string sfx_ic_dwarf_attack_musket_3 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_attack_musket_3");
            public static string sfx_ic_dwarf_attack_pick = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_attack_pick");
            public static string sfx_ic_dwarf_attack_sword_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_attack_sword_1");
            public static string sfx_ic_dwarf_attack_sword_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_attack_sword_2");
            public static string sfx_ic_dwarf_attack_sword_3 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_attack_sword_3");
            public static string sfx_ic_dwarf_climb_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_climb_1");
            public static string sfx_ic_dwarf_climb_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_climb_2");
            public static string sfx_ic_dwarf_climb_3 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_climb_3");
            public static string sfx_ic_dwarf_cook_meal = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_cook_meal");
            public static string sfx_ic_dwarf_craft = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_craft");
            public static string sfx_ic_dwarf_death = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_death");
            public static string sfx_ic_dwarf_eat_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_eat_1");
            public static string sfx_ic_dwarf_eat_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_eat_2");
            public static string sfx_ic_dwarf_eat_3 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_eat_3");
            public static string sfx_ic_dwarf_hurt_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_hurt_1");
            public static string sfx_ic_dwarf_hurt_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_hurt_2");
            public static string sfx_ic_dwarf_jump = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_jump");
            public static string sfx_ic_dwarf_magic_research = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_magic_research");
            public static string sfx_ic_dwarf_magic_research_stereo = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_magic_research_stereo");
            public static string sfx_ic_dwarf_mumble_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_mumble_1");
            public static string sfx_ic_dwarf_mumble_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_mumble_2");
            public static string sfx_ic_dwarf_mumble_3 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_mumble_3");
            public static string sfx_ic_dwarf_mumble_4 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_mumble_4");
            public static string sfx_ic_dwarf_mumble_5 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_mumble_5");
            public static string sfx_ic_dwarf_mumble_6 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_mumble_6");
            public static string sfx_ic_dwarf_ok_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_ok_1");
            public static string sfx_ic_dwarf_ok_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_ok_2");
            public static string sfx_ic_dwarf_ok_3 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_ok_3");
            public static string sfx_ic_dwarf_pick_dirt_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_pick_dirt_1");
            public static string sfx_ic_dwarf_pick_dirt_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_pick_dirt_2");
            public static string sfx_ic_dwarf_pick_dirt_3 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_pick_dirt_3");
            public static string sfx_ic_dwarf_pick_stone_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_pick_stone_1");
            public static string sfx_ic_dwarf_pick_stone_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_pick_stone_2");
            public static string sfx_ic_dwarf_pick_stone_3 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_pick_stone_3");
            public static string sfx_ic_dwarf_pick_wood_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_pick_wood_1");
            public static string sfx_ic_dwarf_pick_wood_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_pick_wood_2");
            public static string sfx_ic_dwarf_pick_wood_3 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_pick_wood_3");
            public static string sfx_ic_dwarf_pleased = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_pleased");
            public static string sfx_ic_dwarf_sleep_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_sleep_1");
            public static string sfx_ic_dwarf_sleep_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_sleep_2");
            public static string sfx_ic_dwarf_sleep_3 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_sleep_3");
            public static string sfx_ic_dwarf_stash_item = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_stash_item");
            public static string sfx_ic_dwarf_stash_money = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_stash_money");
            public static string sfx_ic_dwarf_stockpile = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_stockpile");
            public static string sfx_ic_dwarf_tantrum_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_tantrum_1");
            public static string sfx_ic_dwarf_tantrum_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_tantrum_2");
            public static string sfx_ic_dwarf_tantrum_3 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_dwarf_tantrum_3");
            public static string sfx_ic_elf_angered = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_elf_angered");
            public static string sfx_ic_elf_arrow_hit = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_elf_arrow_hit");
            public static string sfx_ic_elf_mumble_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_elf_mumble_1");
            public static string sfx_ic_elf_mumble_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_elf_mumble_2");
            public static string sfx_ic_elf_mumble_3 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_elf_mumble_3");
            public static string sfx_ic_elf_mumble_4 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_elf_mumble_4");
            public static string sfx_ic_elf_mumble_5 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_elf_mumble_5");
            public static string sfx_ic_elf_mumble_6 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_elf_mumble_6");
            public static string sfx_ic_elf_pleased = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_elf_pleased");
            public static string sfx_ic_elf_shoot_bow = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_elf_shoot_bow");
            public static string sfx_ic_generic_hurt_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_generic_hurt_1");
            public static string sfx_ic_generic_hurt_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_generic_hurt_2");
            public static string sfx_ic_generic_hurt_3 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_generic_hurt_3");
            public static string sfx_ic_goblin_angered = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_goblin_angered");
            public static string sfx_ic_goblin_mumble_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_goblin_mumble_1");
            public static string sfx_ic_goblin_mumble_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_goblin_mumble_2");
            public static string sfx_ic_goblin_mumble_3 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_goblin_mumble_3");
            public static string sfx_ic_goblin_mumble_4 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_goblin_mumble_4");
            public static string sfx_ic_goblin_mumble_5 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_goblin_mumble_5");
            public static string sfx_ic_goblin_mumble_6 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_goblin_mumble_6");
            public static string sfx_ic_goblin_pleased = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_goblin_pleased");
            public static string sfx_ic_moleman_angered = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_moleman_angered");
            public static string sfx_ic_moleman_mumble_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_moleman_mumble_1");
            public static string sfx_ic_moleman_mumble_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_moleman_mumble_2");
            public static string sfx_ic_moleman_mumble_3 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_moleman_mumble_3");
            public static string sfx_ic_moleman_mumble_4 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_moleman_mumble_4");
            public static string sfx_ic_moleman_mumble_5 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_moleman_mumble_5");
            public static string sfx_ic_moleman_mumble_6 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_moleman_mumble_6");
            public static string sfx_ic_moleman_pleased = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_moleman_pleased");
            public static string sfx_ic_necromancer_angered = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_necromancer_angered");
            public static string sfx_ic_necromancer_mumble_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_necromancer_mumble_1");
            public static string sfx_ic_necromancer_mumble_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_necromancer_mumble_2");
            public static string sfx_ic_necromancer_mumble_3 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_necromancer_mumble_3");
            public static string sfx_ic_necromancer_mumble_4 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_necromancer_mumble_4");
            public static string sfx_ic_necromancer_mumble_5 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_necromancer_mumble_5");
            public static string sfx_ic_necromancer_mumble_6 = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_necromancer_mumble_6");
            public static string sfx_ic_necromancer_pleased = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_necromancer_pleased");
            public static string sfx_ic_necromancer_summon = ProgramData.CreatePath("Audio", "oscar", "sfx_ic_necromancer_summon");
            public static string sfx_oc_bat_attack_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_bat_attack_1");
            public static string sfx_oc_bat_hurt_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_bat_hurt_1");
            public static string sfx_oc_bat_neutral_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_bat_neutral_1");
            public static string sfx_oc_bat_neutral_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_bat_neutral_2");
            public static string sfx_oc_chicken_attack = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_chicken_attack");
            public static string sfx_oc_chicken_hurt_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_chicken_hurt_1");
            public static string sfx_oc_chicken_hurt_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_chicken_hurt_2");
            public static string sfx_oc_chicken_lay_egg = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_chicken_lay_egg");
            public static string sfx_oc_chicken_neutral_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_chicken_neutral_1");
            public static string sfx_oc_chicken_neutral_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_chicken_neutral_2");
            public static string sfx_oc_deer_attack = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_deer_attack");
            public static string sfx_oc_deer_hurt_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_deer_hurt_1");
            public static string sfx_oc_deer_neutral_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_deer_neutral_1");
            public static string sfx_oc_deer_neutral_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_deer_neutral_2");
            public static string sfx_oc_frog_attack = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_frog_attack");
            public static string sfx_oc_frog_hurt_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_frog_hurt_1");
            public static string sfx_oc_frog_hurt_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_frog_hurt_2");
            public static string sfx_oc_frog_neutral_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_frog_neutral_1");
            public static string sfx_oc_frog_neutral_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_frog_neutral_2");
            public static string sfx_oc_giant_snake_attack_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_giant_snake_attack_1");
            public static string sfx_oc_giant_snake_hurt_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_giant_snake_hurt_1");
            public static string sfx_oc_giant_snake_neutral_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_giant_snake_neutral_1");
            public static string sfx_oc_giant_snake_neutral_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_giant_snake_neutral_2");
            public static string sfx_oc_giant_spider_attack_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_giant_spider_attack_1");
            public static string sfx_oc_giant_spider_attack_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_giant_spider_attack_2");
            public static string sfx_oc_giant_spider_hurt_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_giant_spider_hurt_1");
            public static string sfx_oc_giant_spider_neutral_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_giant_spider_neutral_1");
            public static string sfx_oc_giant_spider_neutral_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_giant_spider_neutral_2");
            public static string sfx_oc_rabbit_attack = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_rabbit_attack");
            public static string sfx_oc_rabbit_hurt_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_rabbit_hurt_1");
            public static string sfx_oc_rabbit_hurt_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_rabbit_hurt_2");
            public static string sfx_oc_rabbit_neutral_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_rabbit_neutral_1");
            public static string sfx_oc_rabbit_neutral_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_rabbit_neutral_2");
            public static string sfx_oc_scorpion_attack_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_scorpion_attack_1");
            public static string sfx_oc_scorpion_attack_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_scorpion_attack_2");
            public static string sfx_oc_scorpion_hurt_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_scorpion_hurt_1");
            public static string sfx_oc_scorpion_neutral_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_scorpion_neutral_1");
            public static string sfx_oc_scorpion_neutral_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_oc_scorpion_neutral_2");
            public static string sfx_trap_turret_shoot_1 = ProgramData.CreatePath("Audio", "oscar", "sfx_trap_turret_shoot_1");
            public static string sfx_trap_turret_shoot_2 = ProgramData.CreatePath("Audio", "oscar", "sfx_trap_turret_shoot_2");

        }
            public static string chew = ProgramData.CreatePath("Audio", "chew");
            public static string explode = ProgramData.CreatePath("Audio", "explode");
            public static string fire = ProgramData.CreatePath("Audio", "fire");
            public static string gravel = ProgramData.CreatePath("Audio", "gravel");
            public static string hit = Oscar.sfx_ic_dwarf_jump;
            public static string jump = Oscar.sfx_ic_dwarf_jump;
            public static string ouch = ProgramData.CreatePath("Audio", "ouch");
            public static string pick = ProgramData.CreatePath("Audio", "pick");
            public static string river = ProgramData.CreatePath("Audio", "river");
            public static string sword = Oscar.sfx_ic_dwarf_attack_sword_1;
            public static string dig = ProgramData.CreatePath("Audio", "dig");
            public static string whoosh = ProgramData.CreatePath("Audio", "whoosh");
            public static string cash = Oscar.sfx_ic_dwarf_stash_money;
            public static string change = Oscar.sfx_ic_dwarf_stash_money;
            public static string bird = ProgramData.CreatePath("Audio", "bird");
            public static string pluck = ProgramData.CreatePath("Audio", "pluck");
            public static string trap = ProgramData.CreatePath("Audio", "trap");
            public static string vegetation_break = ProgramData.CreatePath("Audio", "vegetation_break");
            public static string hammer = ProgramData.CreatePath("Audio", "hammer");
            public static string wurp = ProgramData.CreatePath("Audio", "wurp");
            public static string tinkle = ProgramData.CreatePath("Audio", "tinkle");
            public static string powerup = ProgramData.CreatePath("Audio", "powerup");
            public static string frog = ProgramData.CreatePath("Audio", "frog");
            public static string bunny = ProgramData.CreatePath("Audio", "bunny");
            public static string demon_attack = ProgramData.CreatePath("Audio", "demon_attack");
            public static string demon0 = ProgramData.CreatePath("Audio", "demon0");
            public static string demon1 = ProgramData.CreatePath("Audio", "demon1");
            public static string demon2= ProgramData.CreatePath("Audio", "demon2");
            public static string demon3 = ProgramData.CreatePath("Audio", "demon3");
            public static string elf0 = ProgramData.CreatePath("Audio", "elf0");
            public static string elf1 = ProgramData.CreatePath("Audio", "elf1");
            public static string elf2 = ProgramData.CreatePath("Audio", "elf2");
            public static string elf3 = ProgramData.CreatePath("Audio", "elf3");
            public static string mole0 = ProgramData.CreatePath("Audio", "mole0");
            public static string mole1 = ProgramData.CreatePath("Audio", "mole1");
            public static string mole2 = ProgramData.CreatePath("Audio", "mole2");
            public static string ok0 = ProgramData.CreatePath("Audio", "ok0");
            public static string ok1 = ProgramData.CreatePath("Audio", "ok1");
            public static string ok2 = ProgramData.CreatePath("Audio", "ok3");
            public static string skel0 = ProgramData.CreatePath("Audio", "skel0");
            public static string skel1 = ProgramData.CreatePath("Audio", "skel1");
            public static string skel2 = ProgramData.CreatePath("Audio", "skel2");
            public static string hiss = ProgramData.CreatePath("Audio", "hiss");
        }
        public class Particles
        {
            public static string gibs = ProgramData.CreatePath("Particles", "gib_particle");
            public static string splash = ProgramData.CreatePath("Particles", "splash");
            public static string blood_particle = ProgramData.CreatePath("Particles", "blood_particle");
            public static string dirt_particle = ProgramData.CreatePath("Particles", "dirt_particle");
            public static string flame = ProgramData.CreatePath("Particles", "flame");
            public static string more_flames = ProgramData.CreatePath("Particles", "moreflames");
            public static string leaf = ProgramData.CreatePath("Particles", "leaf");
            public static string puff = ProgramData.CreatePath("Particles", "puff");
            public static string sand_particle = ProgramData.CreatePath("Particles", "sand_particle");
            public static string splash2 = ProgramData.CreatePath("Particles", "splash2");
            public static string splat = ProgramData.CreatePath("Particles", "splat");
            public static string stone_particle = ProgramData.CreatePath("Particles", "stone_particle");
            public static string green_flame = ProgramData.CreatePath("Particles", "green_flame");
            public static string star_particle = ProgramData.CreatePath("Particles", "bigstar_particle");
            public static string heart = ProgramData.CreatePath("Particles", "heart");
            public static string fireball = ProgramData.CreatePath("Particles", "fireball");
            public static string raindrop = ProgramData.CreatePath("Particles", "raindrop");
            public static string cloud1 = ProgramData.CreatePath("Sky", "cloud1");
            public static string cloud2 = ProgramData.CreatePath("Sky", "cloud2");
            public static string snow_particle = ProgramData.CreatePath("Particles", "snow_particle");
            public static string particles = ProgramData.CreatePath("Particles", "particles.json");
        }
        public class Effects
        {
            public static string shadowcircle = ProgramData.CreatePath("Effects", "shadowcircle");
            public static string selection_circle = ProgramData.CreatePath("Effects", "selection_circle");
            public static string slice = ProgramData.CreatePath("Effects", "slice");
            public static string slash = ProgramData.CreatePath("Effects", "slash");
            public static string claw = ProgramData.CreatePath("Effects", "claw");
            public static string claws = ProgramData.CreatePath("Effects", "claws");
            public static string flash = ProgramData.CreatePath("Effects", "flash");
            public static string rings = ProgramData.CreatePath("Effects", "ring");
            public static string bite = ProgramData.CreatePath("Effects", "bite");
            public static string pierce = ProgramData.CreatePath("Effects", "pierce");
            public static string hit = ProgramData.CreatePath("Effects", "hit");
            public static string explode = ProgramData.CreatePath("Effects", "explode");
            public static string particles = ProgramData.CreatePath("Effects", "particles.json");
        }

        public class World
        {
            public static string biomes = ProgramData.CreatePath("World", "biomes.json");
            public static string races = ProgramData.CreatePath("World", "races.json");
            public static string embarks = ProgramData.CreatePath("World", "embarkments.json");
        }

        public static T LoadFromJson<T>(string asset)
        {
            return FileUtils.LoadJsonFromString<T>(ContentPaths.GetFileAsString(asset));
        }

        public static string GetFileAsString(string asset)
        {
            string text = "";
            using (var stream = TitleContainer.OpenStream("Content" + ProgramData.DirChar + asset))
            {
                using (var reader = new StreamReader(stream))
                {
                    text = reader.ReadToEnd();
                }
            }
            return text;
        }

        public class Text
        {
            public class Templates
            {
                public static string nations_dwarf = ProgramData.CreatePath("Text", "Templates", "nations_dwarf.txt");
                public static string nations_elf = ProgramData.CreatePath("Text", "Templates", "nations_elf.txt");
                public static string nations_goblin = ProgramData.CreatePath("Text", "Templates", "nations_goblin.txt");
                public static string nations_undead = ProgramData.CreatePath("Text", "Templates", "nations_undead.txt");
                public static string mottos = ProgramData.CreatePath("Text", "Templates", "mottos.txt");

                public static string company_exploration = ProgramData.CreatePath("Text", "Templates", "company_exploration.txt");
                public static string company_finance = ProgramData.CreatePath("Text", "Templates", "company_finance.txt");
                public static string company_industrial = ProgramData.CreatePath("Text", "Templates", "company_industrial.txt");
                public static string company_magical = ProgramData.CreatePath("Text", "Templates", "company_magical.txt");
                public static string company_military = ProgramData.CreatePath("Text", "Templates", "company_military.txt");
                public static string worlds = ProgramData.CreatePath("Text", "Templates", "worlds.txt");
                public static string names_dwarf = ProgramData.CreatePath("Text", "Templates", "names_dwarf.txt");
                public static string names_goblin = ProgramData.CreatePath("Text", "Templates", "names_goblin.txt");
                public static string names_elf = ProgramData.CreatePath("Text", "Templates", "names_elf.txt");
                public static string names_undead = ProgramData.CreatePath("Text", "Templates", "names_undead.txt");
                public static string food = ProgramData.CreatePath("Text", "Templates", "foods.txt");
                public static string location = ProgramData.CreatePath("Text", "Templates", "location.txt");
                public static string biography = ProgramData.CreatePath("Text", "Templates", "biography.txt");
                public static string hobby = ProgramData.CreatePath("Text", "Templates", "hobby.txt");
            }
        }

        public class Entities
        {
            public static string mudman_animation = ProgramData.CreatePath("Entities", "Mudman", "mudman_animation.json");
            public static string mudman_projectile = ProgramData.CreatePath("Entities", "Mudman", "mudman-projectile");

            public class Animals
            {
                public static string chicken_animations = ProgramData.CreatePath("Entities", "Animals",
                    "chicken_animation.json");

                public class Bat
                {
                    public static string bat = ProgramData.CreatePath("Entities", "Animals", "bat");
                    public static string bat_animations = ProgramData.CreatePath("Entities", "Animals", "bat_animation.json");
                }

                public class Spider
                {
                    public static string spider = ProgramData.CreatePath("Entities", "Animals", "Spider", "spider");
                    public static string spider_animation = ProgramData.CreatePath("Entities", "Animals", "Spider", "spider_animation.json");
                    public static string webstick = ProgramData.CreatePath("Entities", "Animals", "Spider", "webstick");
                    public static string webshot = ProgramData.CreatePath("Entities", "Animals", "Spider", "webshot");
                }

                public class Birds
                {
                    public static string bird_prefix = ProgramData.CreatePath("Entities", "Animals", "Birds", "bird");
                    public static string bird0 = ProgramData.CreatePath("Entities", "Animals", "Birds", "bird0");
                    public static string bird1 = ProgramData.CreatePath("Entities", "Animals", "Birds", "bird1");
                    public static string bird2 = ProgramData.CreatePath("Entities", "Animals", "Birds", "bird2");
                    public static string bird3 = ProgramData.CreatePath("Entities", "Animals", "Birds", "bird3");
                    public static string bird4 = ProgramData.CreatePath("Entities", "Animals", "Birds", "bird4");
                    public static string bird5 = ProgramData.CreatePath("Entities", "Animals", "Birds", "bird5");
                    public static string bird6 = ProgramData.CreatePath("Entities", "Animals", "Birds", "bird6");
                    public static string bird7 = ProgramData.CreatePath("Entities", "Animals", "Birds", "bird7");

                    // Generates a random bird asset string from bird0 to bird7.
                    public static string GetRandomBird()
                    {
                        return bird_prefix + MathFunctions.Random.Next(8);
                    }
                }

                public class Rabbit
                {
                    public static string rabbit0 = ProgramData.CreatePath("Entities", "Animals", "Rabbit", "rabbit0");
                    public static string rabbit1 = ProgramData.CreatePath("Entities", "Animals", "Rabbit", "rabbit1");
                    public static string rabbit0_animation = ProgramData.CreatePath("Entities", "Animals", "Rabbit", "rabbit0_animation.json");
                    public static string rabbit1_animation = ProgramData.CreatePath("Entities", "Animals", "Rabbit", "rabbit1_animation.json");
                }

                public class Frog
                {
                    public static string frog0 = ProgramData.CreatePath("Entities", "Animals", "Frog", "frog0");
                    public static string frog1 = ProgramData.CreatePath("Entities", "Animals", "Frog", "frog1");
                    public static string frog0_animation = ProgramData.CreatePath("Entities", "Animals", "Frog", "frog0_animation.json");
                    public static string frog1_animation = ProgramData.CreatePath("Entities", "Animals", "Frog", "frog1_animation.json");
                }


                public class Scorpion
                {
                    public static string scorpion = ProgramData.CreatePath("Entities", "Animals", "scorpion");
                    public static string scorption_animation = ProgramData.CreatePath("Entities", "Animals", "scorpion_animation.json");
                }

                public class Deer
                {
                    public static string deer = ProgramData.CreatePath("Entities", "Animals", "Deer", "deer");
                }

                public class Snake
                {
                    public static string snake = ProgramData.CreatePath("Entities", "Animals", "Snake", "snake");
                }
            }

            public class Balloon
            {
                public class Sprites
                {
                    public static string balloon = ProgramData.CreatePath("Entities", "Balloon", "Sprites", "balloon");

                }

            }

            public class Elf
            {
                public class Sprites
                {
                    public static string elf_animation = ProgramData.CreatePath("Entities", "Elf", "Sprites", "elf_animation.json");
                    public static string elf = ProgramData.CreatePath("Entities", "Elf", "Sprites", "elf");
                    public static string elf_bow = ProgramData.CreatePath("Entities", "Elf", "Sprites", "elf-bow");
                    public static string arrow = ProgramData.CreatePath("Entities", "Elf", "Sprites", "arrow");
                }
            }

            public class Dwarf
            {
                public static string dwarf_classes = ProgramData.CreatePath("Entities", "Dwarf", "dwarf_classes.json");
                public static string dwarf = ProgramData.CreatePath("Entities", "Dwarf", "dwarf.json");

                public class Audio
                {
                    public static string dwarfhurt1 = ProgramData.CreatePath("Entities", "Dwarf", "Audio", "dwarfhurt1");
                    public static string dwarfhurt2 = ProgramData.CreatePath("Entities", "Dwarf", "Audio", "dwarfhurt2");
                    public static string dwarfhurt3 = ProgramData.CreatePath("Entities", "Dwarf", "Audio", "dwarfhurt3");
                    public static string dwarfhurt4 = ProgramData.CreatePath("Entities", "Dwarf", "Audio", "dwarfhurt4");

                }
                public class Sprites
                {
                    public static string crafter_hammer = ProgramData.CreatePath("Entities", "Dwarf", "Sprites", "crafter-hammer");
                    public static string crafter = ProgramData.CreatePath("Entities", "Dwarf", "Sprites", "crafter");
                    public static string soldier_axe = ProgramData.CreatePath("Entities", "Dwarf", "Sprites", "soldier-axe");
                    public static string soldier_shield = ProgramData.CreatePath("Entities", "Dwarf", "Sprites", "soldier-shield");
                    public static string soldier = ProgramData.CreatePath("Entities", "Dwarf", "Sprites", "soldier");
                    public static string wizard_staff = ProgramData.CreatePath("Entities", "Dwarf", "Sprites", "wizard-staff");
                    public static string wizard = ProgramData.CreatePath("Entities", "Dwarf", "Sprites", "wizard");
                    public static string worker_pick = ProgramData.CreatePath("Entities", "Dwarf", "Sprites", "worker-pick");
                    public static string worker = ProgramData.CreatePath("Entities", "Dwarf", "Sprites", "worker");

                    public static string worker_animation = ProgramData.CreatePath("Entities", "Dwarf", "Sprites", "worker_animation.json");
                    public static string crafter_animation = ProgramData.CreatePath("Entities", "Dwarf", "Sprites", "crafter_animation.json");
                    public static string wizard_animation = ProgramData.CreatePath("Entities", "Dwarf", "Sprites", "wizard_animation.json");
                    public static string soldier_animation =  ProgramData.CreatePath("Entities", "Dwarf", "Sprites", "soldier_animation.json");

                    public static string fairy = ProgramData.CreatePath("Entities", "Dwarf", "Sprites", "fairy");

                    public static string fairy_animation = ProgramData.CreatePath("Entities", "Dwarf", "Sprites",
                        "fairy_animation.json");

                    public static string musketdwarf_animations = ProgramData.CreatePath("Entities", "Dwarf", "Sprites", "musket_animation.json");
                    public static string musket = ProgramData.CreatePath("Entities", "Dwarf", "Sprites", "musket");
                }

            }

            public class DwarfObjects
            {
                public static string coinpiles = ProgramData.CreatePath("Entities", "DwarfObjects", "coinpiles");
                public static string beartrap = ProgramData.CreatePath("Entities", "DwarfObjects", "beartrap");
                public static string underconstruction = ProgramData.CreatePath("Entities", "DwarfObjects", "underconstruction");
                public static string constructiontape = ProgramData.CreatePath("Entities", "DwarfObjects", "constructiontape");
                public static string fence = ProgramData.CreatePath("Entities", "DwarfObjects", "fence");
                public static string crafts = ProgramData.CreatePath("Entities", "DwarfObjects", "crafts");
                public static string trinkets_carve_insets_bone = ProgramData.CreatePath("newgui", "trinkets-carve-insets-bone");
                public static string trinkets_carve_insets = ProgramData.CreatePath("newgui", "trinkets-carve-insets");
                public static string trinkets_cast_insets = ProgramData.CreatePath("newgui", "trinkets-cast-insets");
                public static string trinkets_cast = ProgramData.CreatePath("newgui", "trinkets-cast");
                public static string trinkets_sculpt_insets = ProgramData.CreatePath("newgui", "trinkets-sculpt-insets");
                public static string trinkets_sculpt = ProgramData.CreatePath("newgui", "trinkets-sculpt");
                public static string trinkets_carve = ProgramData.CreatePath("newgui", "trinkets-carve");
            }

            public class Furniture
            {
                public static string bedtex = ProgramData.CreatePath("Entities", "Furniture", "bedtex");
                public static string interior_furniture = ProgramData.CreatePath("Entities", "Furniture", "interior_furniture");
                public static string bookshelf = ProgramData.CreatePath("Entities", "Furniture", "bookshelf");
            }
            public class Goblin
            {
                public static string goblin = ProgramData.CreatePath("Entities", "Goblin",  "goblin.json");
                public static string goblin_classes = ProgramData.CreatePath("Entities", "Goblin", "goblin_classes.json"); 
                public class Sprites
                {
                    public static string goblin_withsword = ProgramData.CreatePath("Entities", "Goblin", "Sprites", "gob-withsword");
                    public static string goblin_animations = ProgramData.CreatePath("Entities", "Goblin", "Sprites", "goblin_animation.json"); 
                }

                public class Audio
                {
                    public static string goblinhurt1 = ProgramData.CreatePath("Entities", "Goblin", "Audio", "goblinhurt1");
                    public static string goblinhurt2 = ProgramData.CreatePath("Entities", "Goblin", "Audio", "goblinhurt2");
                    public static string goblinhurt3 = ProgramData.CreatePath("Entities", "Goblin", "Audio", "goblinhurt3");
                    public static string goblinhurt4 = ProgramData.CreatePath("Entities", "Goblin", "Audio", "goblinhurt4");

                }
            }

            public class Skeleton
            {
                public static string skeleton = ProgramData.CreatePath("Entities", "Skeleton", "skeleton.json"); 
                
                public class Sprites
                {
                    public static string skele = ProgramData.CreatePath("Entities", "Skeleton", "skele");
                    public static string necro = ProgramData.CreatePath("Entities", "Skeleton", "necro");
                }
            }
            public class Plants
            {
                public static string berrybush = ProgramData.CreatePath("Entities", "Plants", "berrybush");
                public static string deadbush = ProgramData.CreatePath("Entities", "Plants", "deadbush");
                public static string flower = ProgramData.CreatePath("Entities", "Plants", "flower");
                public static string frostgrass = ProgramData.CreatePath("Entities", "Plants", "frostgrass");
                public static string gnarled = ProgramData.CreatePath("Entities", "Plants", "gnarled");
                public static string grass = ProgramData.CreatePath("Entities", "Plants", "grass");
                public static string mushroom = ProgramData.CreatePath("Entities", "Plants", "mushroom");
                public static string cavemushroom = ProgramData.CreatePath("Entities", "Plants", "caveshroom");
                public static string palm = ProgramData.CreatePath("Entities", "Plants", "palm");
                public static string pine = ProgramData.CreatePath("Entities", "Plants", "pine");
                public static string shrub = ProgramData.CreatePath("Entities", "Plants", "shrub");
                public static string snowpine = ProgramData.CreatePath("Entities", "Plants", "snowpine");
                public static string vine = ProgramData.CreatePath("Entities", "Plants", "vine");
                public static string wheat = ProgramData.CreatePath("Entities", "Plants", "wheat");
                public static string appletree = ProgramData.CreatePath("Entities", "Plants", "appletree");
            }
            public class Resources
            {
                //public static string resources = ProgramData.CreatePath("Entities", "Resources", "resources");
                public static string resources = ProgramData.CreatePath("newgui", "resources");
            }

            public class Moleman
            {
                public static string moleman = ProgramData.CreatePath("Entities", "Moleman", "Moleman.json");
                public static string moleman_animations = ProgramData.CreatePath("Entities", "Moleman","moleman_animation.json");
            }

            public class Demon
            {
                public static string demon = ProgramData.CreatePath("Entities", "Demon", "demon");
                public static string demon_animations = ProgramData.CreatePath("Entities", "Demon", "demon_animation.json");
            }
        }
        public class Fonts
        {
            public static string Default = ProgramData.CreatePath("Fonts", "font1-w-2x");
            public static string Small = ProgramData.CreatePath("Fonts", "font1-w");
            public static string Title = ProgramData.CreatePath("Fonts", "font1-w-4x");

        }
        public class Gradients
        {
            public static string ambientgradient = ProgramData.CreatePath("Gradients", "ambientgradient");
            public static string skygradient = ProgramData.CreatePath("Gradients", "skygradient");
            public static string sungradient = ProgramData.CreatePath("Gradients", "sungradient");
            public static string torchgradient = ProgramData.CreatePath("Gradients", "torchgradient");
            public static string shoregradient = ProgramData.CreatePath("Gradients", "shoregradient");
        }
        public class GUI
        {
            public static string gui_widgets = ProgramData.CreatePath("GUI", "gui_widgets");
            public static string icons = ProgramData.CreatePath("GUI", "icons");
            public static string indicators = ProgramData.CreatePath("GUI", "indicators");
            public static string map_icons = ProgramData.CreatePath("GUI", "map_icons");
            public static string pointers = ProgramData.CreatePath("GUI", "pointers");
            public static string room_icons = ProgramData.CreatePath("GUI", "room_icons");
            public static string gui_minimap = ProgramData.CreatePath("GUI", "gui_minimap");
            public static string dorf_diplo = ProgramData.CreatePath("GUI", "diplo-dorf");
            public static string checker = ProgramData.CreatePath("GUI", "checker");
            public static string background = ProgramData.CreatePath("GUI", "background");
        }
        public class Logos
        {
            public static string companylogo = ProgramData.CreatePath("Logos", "companylogo");
            public static string gamelogo = ProgramData.CreatePath("Logos", "gamelogo");
            public static string grebeardlogo = ProgramData.CreatePath("Logos", "grebeardlogo");
            public static string logos = ProgramData.CreatePath("Logos", "logos");

        }
        public class Models
        {
            public static string sphereLowPoly = ProgramData.CreatePath("Models", "sphereLowPoly");

        }
        public class Music
        {
#if XNA_BUILD
            public static string dwarfcorp = ProgramData.CreatePath("Music", "dwarfcorp");
            public static string dwarfcorp_2 = ProgramData.CreatePath("Music", "dwarfcorp_2");
            public static string dwarfcorp_3 = ProgramData.CreatePath("Music", "dwarfcorp_3");
            public static string dwarfcorp_4 = ProgramData.CreatePath("Music", "dwarfcorp_4");
            public static string dwarfcorp_5 = ProgramData.CreatePath("Music", "dwarfcorp_5");
#else
            public static string dwarfcorp = ProgramData.CreatePath("Music", "dwarfcorp_ogg");
#endif

        }
        public class Shaders
        {

            public static string BloomCombine = ProgramData.CreatePath("Shaders", "BloomCombine");
            public static string BloomExtract = ProgramData.CreatePath("Shaders", "BloomExtract");
            public static string GaussianBlur = ProgramData.CreatePath("Shaders", "GaussianBlur");
            public static string SkySphere = ProgramData.CreatePath("Shaders", "SkySphere");
            public static string TexturedShaders = ProgramData.CreatePath("Shaders", "TexturedShaders");
            public static string FXAA = ProgramData.CreatePath("Shaders", "FXAA");
            public static string Background = ProgramData.CreatePath("Shaders", "Background");
        }
        public class Sky
        {
            public static string day_sky = ProgramData.CreatePath("Sky", "day_sky");
            public static string moon = ProgramData.CreatePath("Sky", "moon");
            public static string night_sky = ProgramData.CreatePath("Sky", "night_sky");
            public static string sun = ProgramData.CreatePath("Sky", "sun");

        }
        public class Terrain
        {
            public static string cartoon_water = ProgramData.CreatePath("Terrain", "cartoon_water");
            public static string foam = ProgramData.CreatePath("Terrain", "foam");
            public static string lava = ProgramData.CreatePath("Terrain", "lava");
            public static string lavafoam = ProgramData.CreatePath("Terrain", "lavafoam");
            public static string terrain_illumination = ProgramData.CreatePath("Terrain", "terrain_illumination");
            public static string terrain_tiles = ProgramData.CreatePath("Terrain", "terrain_tiles");
            public static string terrain_colormap = ProgramData.CreatePath("Terrain", "terrain_colormap");
            public static string water_normal = ProgramData.CreatePath("Terrain", "water_normal");
            public static string water_normal2 = ProgramData.CreatePath("Terrain", "water_normal2");

        }

    }

}