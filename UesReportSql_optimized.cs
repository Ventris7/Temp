namespace Lims.Core.Reports.UesReport;

using Lims.Core.Reports.UesReport.Models;

public static class UesReportSql
{
    public const string Sql = @"
with ues_tasks as materialized (
    select
        t.id as task_id,
        t.object_id as sample_id,
        t.end_date_journal,
        t.result,
        coalesce(
            nullif(t.result ->> 'porosityCoefficientCorrectedForSalt', '')::numeric,
            nullif(t.result ->> 'PorosityCoefficientCorrectedForSalt', '')::numeric,
            nullif(t.result ->> 'porosityCoefficient', '')::numeric,
            nullif(t.result ->> 'PorosityCoefficient', '')::numeric
        ) as porosity_coefficient,
        coalesce(
            nullif(t.result ->> 'saturation100UES', '')::numeric,
            nullif(t.result ->> 'Saturation100UES', '')::numeric
        ) as saturation100_ues,
        coalesce(
            nullif(t.result ->> 'saturation100Temperature', '')::numeric,
            nullif(t.result ->> 'Saturation100Temperature', '')::numeric
        ) as saturation100_temperature,
        coalesce(
            nullif(t.result ->> 'porosityParameter', '')::numeric,
            nullif(t.result ->> 'PorosityParameter', '')::numeric
        ) as porosity_parameter,
        coalesce(
            nullif(t.result ->> 'solutionUES', '')::numeric,
            nullif(t.result ->> 'SolutionUES', '')::numeric
        ) as solution_ues,
        coalesce(
            nullif(t.result ->> 'solutionMineralization', '')::numeric,
            nullif(t.result ->> 'SolutionMineralization', '')::numeric
        ) as solution_mineralization,
        case
            when jsonb_typeof(t.result -> 'centrifugations') = 'array' then t.result -> 'centrifugations'
            when jsonb_typeof(t.result -> 'Centrifugations') = 'array' then t.result -> 'Centrifugations'
            else '[]'::jsonb
        end as centrifugations
    from lims.prcs_task t
    where t.result is not null
      and t.method_name in ('UesResult', 'UES', 'Ues')
      and ((@" + nameof(UesReportFilterModel.FlowIds) + @")::int8[] is null or t.flow_id = any((@" + nameof(UesReportFilterModel.FlowIds) + @")::int8[]))
      and (
            ((@" + nameof(UesReportFilterModel.Statuses) + @")::text[] is null and t.status = 'Завершено')
            or ((@" + nameof(UesReportFilterModel.Statuses) + @")::text[] is not null and t.status = any((@" + nameof(UesReportFilterModel.Statuses) + @")::text[]))
          )
      and (@" + nameof(UesReportFilterModel.EndDateJournalFrom) + @"::date is null or t.end_date_journal >= @" + nameof(UesReportFilterModel.EndDateJournalFrom) + @")
      and (@" + nameof(UesReportFilterModel.EndDateJournalTo) + @"::date is null or t.end_date_journal <= @" + nameof(UesReportFilterModel.EndDateJournalTo) + @")
      and coalesce(
            nullif(t.result ->> 'saturation100UES', ''),
            nullif(t.result ->> 'Saturation100UES', '')
          ) is not null
),
ues_sample_rows as materialized (
    select
        ut.task_id,
        ut.sample_id,
        ut.end_date_journal,
        ut.porosity_coefficient,
        ut.saturation100_ues,
        ut.saturation100_temperature,
        ut.porosity_parameter,
        ut.solution_ues,
        ut.solution_mineralization,
        ut.centrifugations,
        osv.well_id,
        nullif(osv.finder_square, '') as lease_square,
        nullif(osv.finder_area, '') as exploration_area,
        nullif(
            case
                when osv.finder_field is not null and coalesce(osv.finder_field_id, 0) <> 9999 then osv.finder_field
                when osv.finder_square is not null and coalesce(osv.finder_square_id, 0) <> 9999 then osv.finder_square
                else osv.field
            end,
            ''
        ) as field,
        coalesce(nullif(osv.finder_well, ''), nullif(osv.well, '')) as well,
        nullif(osv.lab_num, '') as lab_num,
        nullif(osv.direction, 'не выбрано') as direction,
        osv.top,
        osv.bottom,
        osv.core_out,
        osv.depth,
        osv.depth_total,
        osv.depth_total + coalesce(osv.delta, 0) as depth_total_by_gis,
        nullif(osv.primary_layer, '') as layer,
        nullif(osv.description, '') as lithological_description
    from ues_tasks ut
    join report.obj_sample_view osv on osv.sample_id = ut.sample_id
    where ((@" + nameof(UesReportFilterModel.SampleIds) + @")::int8[] is null or osv.sample_id = any((@" + nameof(UesReportFilterModel.SampleIds) + @")::int8[]))
      and ((@" + nameof(UesReportFilterModel.WellIds) + @")::int8[] is null or osv.well_id = any((@" + nameof(UesReportFilterModel.WellIds) + @")::int8[]))
      and ((@" + nameof(UesReportFilterModel.FieldIds) + @")::int8[] is null or osv.field_id = any((@" + nameof(UesReportFilterModel.FieldIds) + @")::int8[]))
),
measurement_rows as (
    select
        usr.task_id,
        case
            when nullif(coalesce(c.""number"", c.""Number"", c.""measurementNumber"", c.""MeasurementNumber""), '')::int between 1 and 3
                then nullif(coalesce(c.""number"", c.""Number"", c.""measurementNumber"", c.""MeasurementNumber""), '')::int
            else e.ordinality::int
        end as measurement_index,
        nullif(coalesce(c.""turnoverCount"", c.""TurnoverCount""), '')::numeric as turnover_count,
        nullif(coalesce(c.""ues"", c.""UES"", c.""partiallySaturatedUES"", c.""PartiallySaturatedUES""), '')::numeric as partially_saturated_ues,
        nullif(coalesce(c.""temperature"", c.""Temperature""), '')::numeric as temperature,
        nullif(coalesce(c.""waterSaturationCoefficient"", c.""WaterSaturationCoefficient"", c.""waterRetentionCapacityCoefficient"", c.""WaterRetentionCapacityCoefficient""), '')::numeric as water_saturation_coefficient,
        nullif(coalesce(c.""saturatingParameter"", c.""SaturatingParameter""), '')::numeric as saturating_parameter
    from ues_sample_rows usr
    cross join lateral jsonb_array_elements(usr.centrifugations) with ordinality as e(value, ordinality)
    cross join lateral jsonb_to_record(e.value) as c(
        ""number"" text,
        ""Number"" text,
        ""measurementNumber"" text,
        ""MeasurementNumber"" text,
        ""turnoverCount"" text,
        ""TurnoverCount"" text,
        ""ues"" text,
        ""UES"" text,
        ""partiallySaturatedUES"" text,
        ""PartiallySaturatedUES"" text,
        ""temperature"" text,
        ""Temperature"" text,
        ""waterSaturationCoefficient"" text,
        ""WaterSaturationCoefficient"" text,
        ""waterRetentionCapacityCoefficient"" text,
        ""WaterRetentionCapacityCoefficient"" text,
        ""saturatingParameter"" text,
        ""SaturatingParameter"" text
    )
    where e.ordinality <= 3
),
measurements as (
    select
        mr.task_id,

        max(mr.turnover_count) filter (where mr.measurement_index = 1) as measurement1_turnover_count,
        max(mr.partially_saturated_ues) filter (where mr.measurement_index = 1) as measurement1_partially_saturated_ues,
        max(mr.temperature) filter (where mr.measurement_index = 1) as measurement1_temperature,
        max(mr.water_saturation_coefficient) filter (where mr.measurement_index = 1) as measurement1_water_saturation_coefficient,
        max(mr.saturating_parameter) filter (where mr.measurement_index = 1) as measurement1_saturating_parameter,

        max(mr.turnover_count) filter (where mr.measurement_index = 2) as measurement2_turnover_count,
        max(mr.partially_saturated_ues) filter (where mr.measurement_index = 2) as measurement2_partially_saturated_ues,
        max(mr.temperature) filter (where mr.measurement_index = 2) as measurement2_temperature,
        max(mr.water_saturation_coefficient) filter (where mr.measurement_index = 2) as measurement2_water_saturation_coefficient,
        max(mr.saturating_parameter) filter (where mr.measurement_index = 2) as measurement2_saturating_parameter,

        max(mr.turnover_count) filter (where mr.measurement_index = 3) as measurement3_turnover_count,
        max(mr.partially_saturated_ues) filter (where mr.measurement_index = 3) as measurement3_partially_saturated_ues,
        max(mr.temperature) filter (where mr.measurement_index = 3) as measurement3_temperature,
        max(mr.water_saturation_coefficient) filter (where mr.measurement_index = 3) as measurement3_water_saturation_coefficient,
        max(mr.saturating_parameter) filter (where mr.measurement_index = 3) as measurement3_saturating_parameter
    from measurement_rows mr
    group by mr.task_id
),
kpr_candidates as materialized (
    select
        kt.id,
        kt.object_id as sample_id,
        kt.end_date_journal,
        case
            when kt.method_name in ('HeliumPermeabilityResult', 'HeliumPermeability') then 1
            when kt.method_name in ('AirPermeabilityResult', 'AirPermeability') then 2
            when kt.method_name in ('PetrophysicalPropertiesResult', 'PetrophysicalProperties') then 3
            else 100
        end as priority,
        coalesce(
            nullif(kt.result ->> 'permeabilityCoefficient', '')::numeric,
            nullif(kt.result ->> 'PermeabilityCoefficient', '')::numeric
        ) as kpr_value
    from lims.prcs_task kt
    join (select distinct sample_id from ues_sample_rows) s on s.sample_id = kt.object_id
    where kt.result is not null
      and kt.method_name in (
            'HeliumPermeabilityResult', 'HeliumPermeability',
            'AirPermeabilityResult', 'AirPermeability',
            'PetrophysicalPropertiesResult', 'PetrophysicalProperties'
          )
      and ((@" + nameof(UesReportFilterModel.FlowIds) + @")::int8[] is null or kt.flow_id = any((@" + nameof(UesReportFilterModel.FlowIds) + @")::int8[]))
      and (
            ((@" + nameof(UesReportFilterModel.Statuses) + @")::text[] is null and kt.status = 'Завершено')
            or ((@" + nameof(UesReportFilterModel.Statuses) + @")::text[] is not null and kt.status = any((@" + nameof(UesReportFilterModel.Statuses) + @")::text[]))
          )
      and (@" + nameof(UesReportFilterModel.EndDateJournalFrom) + @"::date is null or kt.end_date_journal >= @" + nameof(UesReportFilterModel.EndDateJournalFrom) + @")
      and (@" + nameof(UesReportFilterModel.EndDateJournalTo) + @"::date is null or kt.end_date_journal <= @" + nameof(UesReportFilterModel.EndDateJournalTo) + @")
      and coalesce(
            nullif(kt.result ->> 'permeabilityCoefficient', ''),
            nullif(kt.result ->> 'PermeabilityCoefficient', '')
          ) is not null
),
kpr_one as (
    select distinct on (kc.sample_id)
        kc.sample_id,
        kc.kpr_value
    from kpr_candidates kc
    order by
        kc.sample_id,
        kc.priority,
        kc.end_date_journal desc nulls last,
        kc.id desc
)
select
    usr.task_id as ""TaskId"",
    usr.sample_id as ""SampleId"",
    usr.well_id as ""WellId"",
    coalesce(usr.lease_square, '') as ""LeaseSquare"",
    coalesce(usr.exploration_area, '') as ""ExplorationArea"",
    coalesce(usr.field, '') as ""Field"",
    coalesce(usr.well, '') as ""Well"",
    usr.lab_num as ""LabNum"",
    usr.direction as ""Direction"",
    usr.top as ""Top"",
    usr.bottom as ""Bottom"",
    usr.core_out as ""CoreOut"",
    usr.depth as ""Depth"",
    usr.depth_total as ""DepthTotal"",
    usr.depth_total_by_gis as ""DepthTotalByGis"",
    usr.layer as ""Layer"",
    usr.lithological_description as ""LithologicalDescription"",
    usr.porosity_coefficient as ""PorosityCoefficient"",
    ko.kpr_value as ""PermeabilityCoefficient"",
    usr.saturation100_ues as ""Saturation100UES"",
    usr.saturation100_temperature as ""Saturation100Temperature"",
    usr.porosity_parameter as ""PorosityParameter"",

    m.measurement1_turnover_count as ""Measurement1TurnoverCount"",
    m.measurement1_partially_saturated_ues as ""Measurement1PartiallySaturatedUES"",
    m.measurement1_temperature as ""Measurement1Temperature"",
    m.measurement1_water_saturation_coefficient as ""Measurement1WaterSaturationCoefficient"",
    m.measurement1_saturating_parameter as ""Measurement1SaturatingParameter"",

    m.measurement2_turnover_count as ""Measurement2TurnoverCount"",
    m.measurement2_partially_saturated_ues as ""Measurement2PartiallySaturatedUES"",
    m.measurement2_temperature as ""Measurement2Temperature"",
    m.measurement2_water_saturation_coefficient as ""Measurement2WaterSaturationCoefficient"",
    m.measurement2_saturating_parameter as ""Measurement2SaturatingParameter"",

    m.measurement3_turnover_count as ""Measurement3TurnoverCount"",
    m.measurement3_partially_saturated_ues as ""Measurement3PartiallySaturatedUES"",
    m.measurement3_temperature as ""Measurement3Temperature"",
    m.measurement3_water_saturation_coefficient as ""Measurement3WaterSaturationCoefficient"",
    m.measurement3_saturating_parameter as ""Measurement3SaturatingParameter"",

    usr.solution_ues as ""SolutionUES"",
    usr.solution_mineralization as ""SolutionMineralization""
from ues_sample_rows usr
left join measurements m on m.task_id = usr.task_id
left join kpr_one ko on ko.sample_id = usr.sample_id
order by
    coalesce(usr.lease_square, ''),
    coalesce(usr.exploration_area, ''),
    coalesce(usr.field, ''),
    coalesce(usr.well, ''),
    usr.depth_total,
    usr.task_id;
";
}
