-- Flat SQL for the UES report template "Таблица 2.2.1".
-- One result row = one UES task.
-- Centrifugations are pivoted into fixed columns for measurements 1, 2 and 3.
-- If a measurement row is missing, its columns are returned as NULL.

with filtered_tasks as (
    select
        t.id,
        t.object_id,
        t.flow_id,
        t.method_name,
        t.status,
        t.end_date_journal,
        t.result,
        lower(coalesce(t.method_name, t.result -> 'versionMethod' ->> 'methodName', '')) as method_name_lower
    from lims.prcs_task t
    where t.result is not null
      and ((@FlowIds)::int8[] is null or t.flow_id = any((@FlowIds)::int8[]))
      and (
            ((@Statuses)::text[] is null and t.status = 'Завершено')
            or ((@Statuses)::text[] is not null and t.status = any((@Statuses)::text[]))
          )
      and (@EndDateJournalFrom::date is null or t.end_date_journal >= @EndDateJournalFrom)
      and (@EndDateJournalTo::date is null or t.end_date_journal <= @EndDateJournalTo)
),
classified_tasks as (
    select
        ft.*,
        case
            when ft.method_name_lower in ('ues', 'uesresult')
              or ft.method_name_lower like 'ues%'
              or ft.result ? 'centrifugations'
              or ft.result ? 'Centrifugations'
              or ft.result ? 'saturation100Resistance'
              or ft.result ? 'Saturation100Resistance'
              or ft.result ? 'shellWeight'
              or ft.result ? 'ShellWeight'
                then 'UES'
            when ft.method_name_lower in ('heliumpermeability', 'heliumpermeabilityresult')
              or ft.method_name_lower like 'heliumpermeability%'
              or ft.result ? 'klinkenbergPermeabilityCoefficient'
              or ft.result ? 'KlinkenbergPermeabilityCoefficient'
                then 'HELIUM'
            when ft.method_name_lower in ('airpermeability', 'airpermeabilityresult')
              or ft.method_name_lower like 'airpermeability%'
              or ft.result ? 'airViscosity'
              or ft.result ? 'AirViscosity'
                then 'AIR'
            when ft.method_name_lower in ('petrophysicalproperties', 'petrophysicalpropertiesresult')
              or ft.method_name_lower like 'petrophysicalproperties%'
              or ft.result ? 'porosityCoefficientByWater'
              or ft.result ? 'PorosityCoefficientByWater'
              or ft.result ? 'partiallySaturatedUES'
              or ft.result ? 'PartiallySaturatedUES'
              or ft.result ? 'saturatingParameter'
              or ft.result ? 'SaturatingParameter'
                then 'PETRO'
            else null
        end as task_kind
    from filtered_tasks ft
),
ues_tasks as (
    select
        ct.id as task_id,
        ct.object_id as sample_id,
        ct.end_date_journal,
        ct.result,
        coalesce(
            nullif(coalesce(ct.result ->> 'porosityCoefficientCorrectedForSalt', ct.result ->> 'PorosityCoefficientCorrectedForSalt'), '')::numeric,
            nullif(coalesce(ct.result ->> 'porosityCoefficient', ct.result ->> 'PorosityCoefficient'), '')::numeric
        ) as porosity_coefficient,
        nullif(coalesce(ct.result ->> 'saturation100UES', ct.result ->> 'Saturation100UES'), '')::numeric as saturation100_ues,
        nullif(coalesce(ct.result ->> 'saturation100Temperature', ct.result ->> 'Saturation100Temperature'), '')::numeric as saturation100_temperature,
        nullif(coalesce(ct.result ->> 'porosityParameter', ct.result ->> 'PorosityParameter'), '')::numeric as porosity_parameter,
        nullif(coalesce(ct.result ->> 'solutionUES', ct.result ->> 'SolutionUES'), '')::numeric as solution_ues,
        nullif(coalesce(ct.result ->> 'solutionMineralization', ct.result ->> 'SolutionMineralization'), '')::numeric as solution_mineralization,
        case
            when jsonb_typeof(coalesce(ct.result -> 'centrifugations', ct.result -> 'Centrifugations')) = 'array'
                then coalesce(ct.result -> 'centrifugations', ct.result -> 'Centrifugations')
            else '[]'::jsonb
        end as centrifugations
    from classified_tasks ct
    where ct.task_kind = 'UES'
      and nullif(coalesce(ct.result ->> 'saturation100UES', ct.result ->> 'Saturation100UES'), '') is not null
),
measurement_rows as (
    select
        ut.task_id,
        coalesce(
            nullif(coalesce(e.value ->> 'number', e.value ->> 'Number'), '')::int,
            nullif(coalesce(e.value ->> 'measurementNumber', e.value ->> 'MeasurementNumber'), '')::int,
            e.ordinality::int
        ) as measurement_number,
        nullif(coalesce(
            e.value ->> 'turnoverCount',
            e.value ->> 'TurnoverCount',
            e.value ->> 'turnovers',
            e.value ->> 'Turnovers',
            e.value ->> 'rotationSpeed',
            e.value ->> 'RotationSpeed'
        ), '')::numeric as turnover_count,
        nullif(coalesce(
            e.value ->> 'ues',
            e.value ->> 'UES',
            e.value ->> 'partiallySaturatedUES',
            e.value ->> 'PartiallySaturatedUES'
        ), '')::numeric as partially_saturated_ues,
        nullif(coalesce(
            e.value ->> 'temperature',
            e.value ->> 'Temperature'
        ), '')::numeric as temperature,
        nullif(coalesce(
            e.value ->> 'waterSaturationCoefficient',
            e.value ->> 'WaterSaturationCoefficient',
            e.value ->> 'waterRetentionCapacityCoefficient',
            e.value ->> 'WaterRetentionCapacityCoefficient'
        ), '')::numeric as water_saturation_coefficient,
        nullif(coalesce(
            e.value ->> 'saturatingParameter',
            e.value ->> 'SaturatingParameter'
        ), '')::numeric as saturating_parameter
    from ues_tasks ut
    left join lateral jsonb_array_elements(ut.centrifugations) with ordinality as e(value, ordinality) on true
),
measurements as (
    select
        mr.task_id,

        max(mr.turnover_count) filter (where mr.measurement_number = 1) as measurement1_turnover_count,
        max(mr.partially_saturated_ues) filter (where mr.measurement_number = 1) as measurement1_partially_saturated_ues,
        max(mr.temperature) filter (where mr.measurement_number = 1) as measurement1_temperature,
        max(mr.water_saturation_coefficient) filter (where mr.measurement_number = 1) as measurement1_water_saturation_coefficient,
        max(mr.saturating_parameter) filter (where mr.measurement_number = 1) as measurement1_saturating_parameter,

        max(mr.turnover_count) filter (where mr.measurement_number = 2) as measurement2_turnover_count,
        max(mr.partially_saturated_ues) filter (where mr.measurement_number = 2) as measurement2_partially_saturated_ues,
        max(mr.temperature) filter (where mr.measurement_number = 2) as measurement2_temperature,
        max(mr.water_saturation_coefficient) filter (where mr.measurement_number = 2) as measurement2_water_saturation_coefficient,
        max(mr.saturating_parameter) filter (where mr.measurement_number = 2) as measurement2_saturating_parameter,

        max(mr.turnover_count) filter (where mr.measurement_number = 3) as measurement3_turnover_count,
        max(mr.partially_saturated_ues) filter (where mr.measurement_number = 3) as measurement3_partially_saturated_ues,
        max(mr.temperature) filter (where mr.measurement_number = 3) as measurement3_temperature,
        max(mr.water_saturation_coefficient) filter (where mr.measurement_number = 3) as measurement3_water_saturation_coefficient,
        max(mr.saturating_parameter) filter (where mr.measurement_number = 3) as measurement3_saturating_parameter
    from measurement_rows mr
    group by mr.task_id
),
kpr_tasks as (
    select
        ct.id,
        ct.object_id as sample_id,
        ct.task_kind,
        ct.end_date_journal,
        nullif(coalesce(ct.result ->> 'permeabilityCoefficient', ct.result ->> 'PermeabilityCoefficient'), '')::numeric as kpr_value
    from classified_tasks ct
    where ct.task_kind in ('HELIUM', 'AIR', 'PETRO')
      and nullif(coalesce(ct.result ->> 'permeabilityCoefficient', ct.result ->> 'PermeabilityCoefficient'), '') is not null
),
ues_with_kpr as (
    select
        ut.*,
        kpr.kpr_value as permeability_coefficient
    from ues_tasks ut
    left join lateral (
        select kt.kpr_value
        from kpr_tasks kt
        where kt.sample_id = ut.sample_id
          and kt.kpr_value is not null
        order by
            case
                when kt.task_kind = 'HELIUM' then 1
                when kt.task_kind = 'AIR' then 2
                when kt.task_kind = 'PETRO' then 3
                else 100
            end,
            kt.end_date_journal desc nulls last,
            kt.id desc
        limit 1
    ) kpr on true
),
report_rows as (
    select
        uwk.task_id as "TaskId",
        osv.sample_id as "SampleId",
        osv.well_id as "WellId",
        nullif(osv.finder_square, '') as "LeaseSquare",
        nullif(osv.finder_area, '') as "ExplorationArea",
        nullif(
            case
                when osv.finder_field is not null and coalesce(osv.finder_field_id, 0) <> 9999 then osv.finder_field
                when osv.finder_square is not null and coalesce(osv.finder_square_id, 0) <> 9999 then osv.finder_square
                else osv.field
            end,
            ''
        ) as "Field",
        coalesce(nullif(osv.finder_well, ''), nullif(osv.well, '')) as "Well",
        nullif(osv.lab_num, '') as "LabNum",
        nullif(osv.direction, 'не выбрано') as "Direction",
        osv.top as "Top",
        osv.bottom as "Bottom",
        osv.core_out as "CoreOut",
        osv.depth as "Depth",
        osv.depth_total as "DepthTotal",
        osv.depth_total + coalesce(osv.delta, 0) as "DepthTotalByGis",
        nullif(osv.primary_layer, '') as "Layer",
        nullif(osv.description, '') as "LithologicalDescription",
        uwk.porosity_coefficient as "PorosityCoefficient",
        uwk.permeability_coefficient as "PermeabilityCoefficient",
        uwk.saturation100_ues as "Saturation100UES",
        uwk.saturation100_temperature as "Saturation100Temperature",
        uwk.porosity_parameter as "PorosityParameter",

        m.measurement1_turnover_count as "Measurement1TurnoverCount",
        m.measurement1_partially_saturated_ues as "Measurement1PartiallySaturatedUES",
        m.measurement1_temperature as "Measurement1Temperature",
        m.measurement1_water_saturation_coefficient as "Measurement1WaterSaturationCoefficient",
        m.measurement1_saturating_parameter as "Measurement1SaturatingParameter",

        m.measurement2_turnover_count as "Measurement2TurnoverCount",
        m.measurement2_partially_saturated_ues as "Measurement2PartiallySaturatedUES",
        m.measurement2_temperature as "Measurement2Temperature",
        m.measurement2_water_saturation_coefficient as "Measurement2WaterSaturationCoefficient",
        m.measurement2_saturating_parameter as "Measurement2SaturatingParameter",

        m.measurement3_turnover_count as "Measurement3TurnoverCount",
        m.measurement3_partially_saturated_ues as "Measurement3PartiallySaturatedUES",
        m.measurement3_temperature as "Measurement3Temperature",
        m.measurement3_water_saturation_coefficient as "Measurement3WaterSaturationCoefficient",
        m.measurement3_saturating_parameter as "Measurement3SaturatingParameter",

        uwk.solution_ues as "SolutionUES",
        uwk.solution_mineralization as "SolutionMineralization",
        uwk.end_date_journal
    from ues_with_kpr uwk
    join report.obj_sample_view osv on osv.sample_id = uwk.sample_id
    left join measurements m on m.task_id = uwk.task_id
    where ((@SampleIds)::int8[] is null or osv.sample_id = any((@SampleIds)::int8[]))
      and ((@WellIds)::int8[] is null or osv.well_id = any((@WellIds)::int8[]))
      and ((@FieldIds)::int8[] is null or osv.field_id = any((@FieldIds)::int8[]))
)
select
    "TaskId",
    "SampleId",
    "WellId",
    coalesce("LeaseSquare", '') as "LeaseSquare",
    coalesce("ExplorationArea", '') as "ExplorationArea",
    coalesce("Field", '') as "Field",
    coalesce("Well", '') as "Well",
    "LabNum",
    "Direction",
    "Top",
    "Bottom",
    "CoreOut",
    "Depth",
    "DepthTotal",
    "DepthTotalByGis",
    "Layer",
    "LithologicalDescription",
    "PorosityCoefficient",
    "PermeabilityCoefficient",
    "Saturation100UES",
    "Saturation100Temperature",
    "PorosityParameter",

    "Measurement1TurnoverCount",
    "Measurement1PartiallySaturatedUES",
    "Measurement1Temperature",
    "Measurement1WaterSaturationCoefficient",
    "Measurement1SaturatingParameter",

    "Measurement2TurnoverCount",
    "Measurement2PartiallySaturatedUES",
    "Measurement2Temperature",
    "Measurement2WaterSaturationCoefficient",
    "Measurement2SaturatingParameter",

    "Measurement3TurnoverCount",
    "Measurement3PartiallySaturatedUES",
    "Measurement3Temperature",
    "Measurement3WaterSaturationCoefficient",
    "Measurement3SaturatingParameter",

    "SolutionUES",
    "SolutionMineralization"
from report_rows
order by
    coalesce("LeaseSquare", ''),
    coalesce("ExplorationArea", ''),
    coalesce("Field", ''),
    coalesce("Well", ''),
    "DepthTotal",
    "TaskId";
