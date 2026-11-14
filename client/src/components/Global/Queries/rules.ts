import { useQuery } from '@tanstack/react-query';
import type { AttributeInfoDto, RuleDto, RuleTypeInfoDto } from '../../../api/dtos';
import { api } from '../../../api/axiosInstance';
import { apiRoutes } from '../../../constants/apiRoutes';
import { queryKeys } from '../../../constants/queryKeys';

export const fetchRules = async (sessionId: string) => (await api.get<RuleDto[]>(apiRoutes.rules.list(sessionId))).data;

export const useRulesListQuery = (sessionId: string) =>
  useQuery({
    queryKey: queryKeys.rules.list(sessionId),
    queryFn: () => fetchRules(sessionId),
    enabled: Boolean(sessionId),
  });

/** There is no `GET rules/{id}`; the rule is taken from the session's list. */
export const useRuleQuery = (sessionId: string, ruleId: string) =>
  useQuery({
    queryKey: queryKeys.rules.list(sessionId),
    queryFn: () => fetchRules(sessionId),
    enabled: Boolean(sessionId && ruleId),
    select: (rules) => rules.find((rule) => rule.id === ruleId) ?? null,
  });

export const useRuleTypesQuery = () =>
  useQuery({
    queryKey: queryKeys.rules.types(),
    queryFn: async () => (await api.get<RuleTypeInfoDto[]>(apiRoutes.rules.ruleTypes())).data,
  });

/** Attribute keys for rule parameter selects (`sessions/{id}/attributes`). */
export const useSessionAttributesQuery = (sessionId: string) =>
  useQuery({
    queryKey: queryKeys.participants.attributes(sessionId),
    queryFn: async () => (await api.get<AttributeInfoDto[]>(apiRoutes.participants.attributes(sessionId))).data,
    enabled: Boolean(sessionId),
  });
