import type { AllocationDto, MovesDto, PinAllDto, PinDto, ValidationReportDto } from '../../../../api/dtos';
import { api } from '../../../../api/axiosInstance';
import { apiRoutes } from '../../../../constants/apiRoutes';

export const validateMoves = async (allocationId: string, moves: MovesDto) => (await api.post<ValidationReportDto>(apiRoutes.allocations.validateMoves(allocationId), moves)).data;

export const applyMoves = async (allocationId: string, moves: MovesDto) => (await api.post<AllocationDto>(apiRoutes.allocations.applyMoves(allocationId), moves)).data;

export const pinAll = async (allocationId: string, dto: PinAllDto) => (await api.post<PinDto[]>(apiRoutes.pins.pinAll(allocationId), dto)).data;
