function rndseed(seed)
	math.randomseed(seed)
	math.random(); math.random(); math.random() --пропускаем первые не очень рандомные числа
end

rndseed(2);

P={a1={}, b1={}, v1={}, b2={}, v2={}}

function R(A)
	local x=math.random(1, A)
	local s=1-2*math.random(0, 1)
	return s*x
end

Path="../Files/Temp/"

function load_coeffs()
	local t,err = table.load(Path.."coeffs.lua")
	P=t
end

function generate_coeffs(lengths)
	
	rndseed(os.time());
	for i=1,#lengths do
		
		--angle
		P.a1[i]=R(i*3) --rate of change 
		P.b1[i]=1/math.random(1, 10) --oscilation amplitude 
		P.v1[i]=R(5) --frequency
		--length
		P.b2[i]=0--1/math.random(5, 20) --oscilation amplitude
		P.v2[i]=R(i*3) --frequency
		
	end
	
	table.save( P, Path.."coeffs.lua" )
end

function sgn(x)
	return x>0 and 1 or x<0 and -1 or 0
end

function Z(x)  
	local x=math.sin(x)
	local s=sgn(x)
	return s*math.pow(s*x, 1)
end

function use_coeffs(T, lengths)
	
	for i=1,#lengths do
		
		local j=i-1
		
		setA(j, getA(j)+P.a1[i]*(1+P.b1[i]*Z(P.v1[i]*T)))
		setL(j, lengths[i]*(1+P.b2[i]*Z(P.v2[i]*T)))
		
	end
	
end

coeffs_mode="generate"

function init_coeffs(lengths)
	if (coeffs_mode=="generate") then generate_coeffs(lengths)
		else if (coeffs_mode=="load") then load_coeffs(lengths) 
		end
	end
end
		